"""Search for a robust fixed parameter vector across the full topology matrix.

This script evaluates a small set of fixed-vector candidates (no optimization)
and picks the one that minimizes instability across networks/runs.
"""

from __future__ import annotations

import argparse
import json
from copy import deepcopy
from pathlib import Path

from src.research.batch_research_runner import run_batch_research_study


MATRIX_40 = (
    "10x60,10x90,10x120,10x160,14x70,14x100,14x140,14x180,"
    "18x80,18x120,18x160,18x210,24x100,24x140,24x190,24x240,"
    "34x120,34x170,34x220,34x280,48x150,48x210,48x270,48x330,"
    "64x170,64x230,64x290,64x350,80x200,80x260,80x320,80x380,"
    "96x220,96x280,96x340,96x400,120x250,120x320,120x390,120x460"
)


def _candidate_patches() -> list[tuple[str, dict]]:
    return [
        ("baseline", {}),
        (
            "r1",
            {
                "qForward": 160,
                "deliveryProbability": 0.72,
                "rootSourceCharge": 1800,
                "chargeSpreadFactor": 0.32,
                "decayPercent": 0.10,
                "decayIntervalSteps": 80,
            },
        ),
        (
            "r2",
            {
                "qForward": 170,
                "deliveryProbability": 0.68,
                "rootSourceCharge": 1700,
                "penaltyLambda": 32,
                "switchHysteresis": 8,
                "linkLearningRate": 0.28,
            },
        ),
        (
            "r3",
            {
                "qForward": 150,
                "deliveryProbability": 0.75,
                "rootSourceCharge": 1900,
                "chargeDropPerHop": 90,
                "chargeSpreadFactor": 0.35,
                "penaltyLambda": 35,
                "decayPercent": 0.09,
            },
        ),
        (
            "r4",
            {
                "qForward": 180,
                "deliveryProbability": 0.80,
                "rootSourceCharge": 1700,
                "chargeSpreadFactor": 0.30,
                "decayIntervalSteps": 90,
                "decayPercent": 0.08,
            },
        ),
        (
            "r5",
            {
                "qForward": 220,
                "deliveryProbability": 0.65,
                "rootSourceCharge": 1800,
                "penaltyLambda": 45,
                "switchHysteresis": 12,
                "chargeSpreadFactor": 0.22,
            },
        ),
        (
            "r6_try2_weighted",
            {
                "qForward": 443,
                "deliveryProbability": 0.21,
                "rootSourceCharge": 1808,
                "penaltyLambda": 64,
                "switchHysteresis": 35,
                "switchHysteresisRatio": 0.07,
                "chargeDropPerHop": 94,
                "chargeSpreadFactor": 0.08,
                "decayIntervalSteps": 91,
                "decayPercent": 0.22,
                "linkMemory": 0.872,
                "linkLearningRate": 0.50,
                "linkBonusMax": 50,
            },
        ),
        (
            "r7",
            {
                "qForward": 140,
                "deliveryProbability": 0.85,
                "rootSourceCharge": 2000,
                "penaltyLambda": 25,
                "switchHysteresis": 6,
                "switchHysteresisRatio": 0.05,
                "chargeSpreadFactor": 0.40,
                "decayIntervalSteps": 120,
                "decayPercent": 0.06,
                "linkLearningRate": 0.30,
            },
        ),
        (
            "r8",
            {
                "qForward": 200,
                "deliveryProbability": 0.70,
                "rootSourceCharge": 1600,
                "chargeSpreadFactor": 0.26,
                "linkMemory": 0.94,
                "linkLearningRate": 0.20,
            },
        ),
        (
            "r9",
            {
                "qForward": 130,
                "deliveryProbability": 0.90,
                "rootSourceCharge": 2200,
                "penaltyLambda": 20,
                "switchHysteresis": 5,
                "chargeDropPerHop": 70,
                "chargeSpreadFactor": 0.45,
                "decayIntervalSteps": 150,
                "decayPercent": 0.05,
            },
        ),
    ]


def _evaluate_candidate(base_config: dict, patch: dict, *, seed_count: int, rounds: int, workers: int) -> dict:
    cfg = deepcopy(base_config)
    cfg.update(patch)

    request = {
        "baseConfig": cfg,
        "seedCount": seed_count,
        "optimizationIterations": 0,
        "roundsPerCheck": rounds,
        "matrixText": MATRIX_40,
        "parallelWorkers": workers,
    }

    result = run_batch_research_study(request)

    verdict_counts = {"STABLE": 0, "OSCILLATING": 0, "UNSTABLE": 0}
    for run in result.get("evaluationRuns", []):
        verdict = str(run.get("verdict", "UNSTABLE"))
        verdict_counts[verdict] = verdict_counts.get(verdict, 0) + 1

    run_total = max(1, len(result.get("evaluationRuns", [])))
    nonstable_run_share = (
        verdict_counts.get("OSCILLATING", 0) + verdict_counts.get("UNSTABLE", 0)
    ) / run_total

    networks = result.get("networks", [])
    nonstable_networks = [
        net
        for net in networks
        if str((net.get("best") or {}).get("verdict", "UNSTABLE")) != "STABLE"
    ]

    worst_score = min(float((net.get("best") or {}).get("avgScore", 0.0)) for net in networks)
    mean_score = (
        sum(float((net.get("best") or {}).get("avgScore", 0.0)) for net in networks)
        / max(1, len(networks))
    )

    return {
        "config": cfg,
        "request": request,
        "result": result,
        "nonstableNetworkCount": len(nonstable_networks),
        "nonstableRunShare": nonstable_run_share,
        "worstScore": worst_score,
        "meanScore": mean_score,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Search robust fixed vector across topology matrix")
    parser.add_argument(
        "--baseline-request",
        type=Path,
        default=Path("../_theoreme_ai_search/try_3_baseline/request_baseline_fixed_vector.json"),
        help="Path to baseline request JSON",
    )
    parser.add_argument(
        "--seed-count",
        type=int,
        default=2,
        help="Seed count for quick search evaluations",
    )
    parser.add_argument(
        "--rounds",
        type=int,
        default=300,
        help="Rounds per check",
    )
    parser.add_argument(
        "--workers",
        type=int,
        default=5,
        help="Parallel workers",
    )
    parser.add_argument(
        "--out-request",
        type=Path,
        default=Path("../_theoreme_ai_search/try_3_baseline/request_ideal_candidate.json"),
        help="Where to save selected robust request",
    )
    parser.add_argument(
        "--out-summary",
        type=Path,
        default=Path("../_theoreme_ai_search/try_3_baseline/robust_search_summary.json"),
        help="Where to save search summary",
    )

    args = parser.parse_args()

    baseline_request = json.loads(args.baseline_request.read_text(encoding="utf-8"))
    base_config = (baseline_request.get("baseConfig") or {}).copy()

    records: list[dict] = []
    for name, patch in _candidate_patches():
        scored = _evaluate_candidate(
            base_config,
            patch,
            seed_count=max(1, int(args.seed_count)),
            rounds=max(20, int(args.rounds)),
            workers=max(1, int(args.workers)),
        )
        record = {
            "name": name,
            "nonstableNetworkCount": scored["nonstableNetworkCount"],
            "nonstableRunShare": scored["nonstableRunShare"],
            "worstScore": scored["worstScore"],
            "meanScore": scored["meanScore"],
            "config": scored["config"],
        }
        records.append(record)
        print(
            f"DONE {name:16s} "
            f"nonstable_networks={record['nonstableNetworkCount']:2d} "
            f"nonstable_runs={record['nonstableRunShare']*100:6.2f}% "
            f"worst={record['worstScore']:6.2f} mean={record['meanScore']:6.2f}"
        )

    records.sort(
        key=lambda rec: (
            rec["nonstableNetworkCount"],
            rec["nonstableRunShare"],
            -rec["worstScore"],
            -rec["meanScore"],
        )
    )

    best = records[0]
    out_request = {
        "baseConfig": best["config"],
        "seedCount": 10,
        "optimizationIterations": 0,
        "roundsPerCheck": max(20, int(args.rounds)),
        "matrixText": MATRIX_40,
        "parallelWorkers": max(1, int(args.workers)),
    }

    args.out_request.parent.mkdir(parents=True, exist_ok=True)
    args.out_request.write_text(json.dumps(out_request, ensure_ascii=False, indent=2), encoding="utf-8")

    args.out_summary.parent.mkdir(parents=True, exist_ok=True)
    args.out_summary.write_text(json.dumps({"ranked": records}, ensure_ascii=False, indent=2), encoding="utf-8")

    print("TOP_CANDIDATES")
    for rec in records[:5]:
        print(
            f"{rec['name']:16s} "
            f"ns_n={rec['nonstableNetworkCount']:2d} "
            f"ns_run={rec['nonstableRunShare']*100:6.2f}% "
            f"worst={rec['worstScore']:6.2f} "
            f"mean={rec['meanScore']:6.2f}"
        )

    print(f"SAVED_REQUEST {args.out_request}")
    print(f"SAVED_SUMMARY {args.out_summary}")


if __name__ == "__main__":
    main()
