"""Purpose: Build compact chart-ready report data from raw batch study outputs."""

from __future__ import annotations

from src.research.metrics.pass_metrics import build_compact_pass_metrics

AXIOM_KEYS = ["a5", "a6", "a7"]
THEOREM_KEYS = ["lemma41", "lemma42", "lemma43"]
ALL_CHECK_KEYS = [*AXIOM_KEYS, *THEOREM_KEYS]


def _compact_runs(runs: list[dict]) -> list[dict]:
    return [
        {
            "networkId": run.get("networkId"),
            "score": float(run.get("score", 0)),
            "verdict": run.get("verdict"),
            "params": run.get("params", {}),
        }
        for run in (runs or [])
    ]


def _build_dependency_data(runs: list[dict], parameter_keys: list[str]) -> dict:
    result: dict[str, list[dict]] = {}

    for param in parameter_keys:
        result[param] = [
            {
                "x": float(run.get("params", {}).get(param, 0)),
                "y": float(run.get("score", 0)),
                "verdict": run.get("verdict"),
            }
            for run in runs
        ]

    return result


def _average_metric(items: list[dict], key: str) -> float:
    if not items:
        return 0.0
    total = sum(float(item.get(key, 0)) for item in items)
    return total / len(items)


def _build_chart_series(snapshots: list[dict]) -> dict:
    source = snapshots or []
    return {
        "duplicates": [float(item.get("downDuplicates", 0)) for item in source],
        "coveragePercent": [float(item.get("downCoverage", 0)) * 100 for item in source],
        "eligibleCount": [float(item.get("eligibleCount", 0)) for item in source],
        "parentChanges": [float(item.get("parentChanges", 0)) for item in source],
        "flappingNodes": [float(item.get("flappingNodes", 0)) for item in source],
        "upHops": [float(item.get("upHops", 0)) for item in source],
        "upUpdates": [float(item.get("upUpdates", 0)) for item in source],
        "propagationDeliveries": [float(item.get("propagationDeliveries", 0)) for item in source],
        "spreadUpdates": [float(item.get("spreadUpdates", 0)) for item in source],
        "assumptionsPassBinary": [1.0 if item.get("assumptionsPass") is True else 0.0 for item in source],
        "theoremPassBinary": [1.0 if item.get("theoremPass") is True else 0.0 for item in source],
        "allChecksBinary": [
            1.0
            if all(item.get(key) is True for key in ALL_CHECK_KEYS)
            else 0.0
            for item in source
        ],
        "a5Binary": [1.0 if item.get("a5") is True else 0.0 for item in source],
        "a6Binary": [1.0 if item.get("a6") is True else 0.0 for item in source],
        "a7Binary": [1.0 if item.get("a7") is True else 0.0 for item in source],
        "lemma41Binary": [1.0 if item.get("lemma41") is True else 0.0 for item in source],
        "lemma42Binary": [1.0 if item.get("lemma42") is True else 0.0 for item in source],
        "lemma43Binary": [1.0 if item.get("lemma43") is True else 0.0 for item in source],
    }


def _first_round_where(snapshots: list[dict], predicate) -> int | None:
    for item in snapshots:
        if predicate(item):
            return int(item.get("round", 0))
    return None


def _sustained_from_round(snapshots: list[dict], predicate) -> int | None:
    if not snapshots:
        return None

    candidate: int | None = None
    tail_all_true = True

    for item in reversed(snapshots):
        if tail_all_true and predicate(item):
            candidate = int(item.get("round", 0))
            continue
        tail_all_true = False

    return candidate


def _pass_rate(snapshots: list[dict], key: str) -> float:
    if not snapshots:
        return 0.0
    passed = sum(1 for item in snapshots if item.get(key) is True)
    return passed / len(snapshots)


def _build_theorem_activation_stats(snapshots: list[dict]) -> dict:
    def all_checks(item: dict) -> bool:
        return all(item.get(key) is True for key in ALL_CHECK_KEYS)

    stats = {
        "firstAssumptionsPassRound": _first_round_where(
            snapshots,
            lambda item: item.get("assumptionsPass") is True,
        ),
        "firstTheoremPassRound": _first_round_where(
            snapshots,
            lambda item: item.get("theoremPass") is True,
        ),
        "firstAllChecksPassRound": _first_round_where(snapshots, all_checks),
        "sustainedAssumptionsPassFromRound": _sustained_from_round(
            snapshots,
            lambda item: item.get("assumptionsPass") is True,
        ),
        "sustainedTheoremPassFromRound": _sustained_from_round(
            snapshots,
            lambda item: item.get("theoremPass") is True,
        ),
        "sustainedAllChecksPassFromRound": _sustained_from_round(
            snapshots,
            all_checks,
        ),
        "passRate": {
            "assumptionsPass": _pass_rate(snapshots, "assumptionsPass"),
            "theoremPass": _pass_rate(snapshots, "theoremPass"),
            "a5": _pass_rate(snapshots, "a5"),
            "a6": _pass_rate(snapshots, "a6"),
            "a7": _pass_rate(snapshots, "a7"),
            "lemma41": _pass_rate(snapshots, "lemma41"),
            "lemma42": _pass_rate(snapshots, "lemma42"),
            "lemma43": _pass_rate(snapshots, "lemma43"),
        },
    }

    first_by_check: dict[str, int | None] = {}
    sustained_by_check: dict[str, int | None] = {}
    for key in ["assumptionsPass", "theoremPass", *ALL_CHECK_KEYS]:
        first_by_check[key] = _first_round_where(
            snapshots,
            lambda item, k=key: item.get(k) is True,
        )
        sustained_by_check[key] = _sustained_from_round(
            snapshots,
            lambda item, k=key: item.get(k) is True,
        )

    stats["firstByCheckRound"] = first_by_check
    stats["sustainedByCheckRound"] = sustained_by_check
    return stats


def build_report_data(study: dict) -> dict:
    runs = _compact_runs(study.get("evaluationRuns", []))
    tuned_parameters = [
        {"key": item.get("key"), "label": item.get("label")}
        for item in (study.get("tunedParameters", []) or [])
    ]
    parameter_keys = [item["key"] for item in tuned_parameters]

    recommendations = [
        {
            "networkId": network.get("id"),
            "label": network.get("label"),
            "nodeCount": network.get("nodeCount"),
            "linkRadius": network.get("linkRadius"),
            "optimizer": "Adaptive gradient search + plateau escape",
            "avgScore": float(network.get("best", {}).get("avgScore", 0)),
            "stableRatio": float(network.get("best", {}).get("stableRatio", 0)),
            "bestSeed": network.get("best", {}).get("seed"),
            "verdict": network.get("best", {}).get("verdict", "UNSTABLE"),
            "bestParameters": network.get("best", {}).get("parameters", {}),
        }
        for network in (study.get("networks", []) or [])
    ]

    network_pass_metrics = [
        build_compact_pass_metrics((network.get("best", {}) or {}).get("snapshots", []))
        for network in (study.get("networks", []) or [])
    ]

    pass_summary = {
        "avgTheoremPassRate": _average_metric(network_pass_metrics, "theoremPassRate"),
        "avgAssumptionsPassRate": _average_metric(network_pass_metrics, "assumptionsPassRate"),
        "avgA5PassRate": _average_metric(network_pass_metrics, "a5PassRate"),
        "avgA6PassRate": _average_metric(network_pass_metrics, "a6PassRate"),
        "avgA7PassRate": _average_metric(network_pass_metrics, "a7PassRate"),
        "avgLemma41PassRate": _average_metric(network_pass_metrics, "lemma41PassRate"),
        "avgLemma42PassRate": _average_metric(network_pass_metrics, "lemma42PassRate"),
        "avgLemma43PassRate": _average_metric(network_pass_metrics, "lemma43PassRate"),
    }

    metadata = {
        "generatedAt": (study.get("metadata") or {}).get("generatedAt"),
        "totalRuns": float((study.get("metadata") or {}).get("totalRuns", len(runs))),
        "networkCount": float(
            (study.get("metadata") or {}).get(
                "topologyCount", len(study.get("networks", []) or [])
            )
        ),
        "optimizationIterations": float((study.get("metadata") or {}).get("optimizationIterations", 0)),
        "tunedParameterCount": len(tuned_parameters),
        "seedStart": float((study.get("metadata") or {}).get("seedStart", 0)),
        "seedCount": float((study.get("metadata") or {}).get("seedCount", 0)),
        "roundsPerCheck": float((study.get("metadata") or {}).get("roundsPerCheck", 0)),
        "passSummary": pass_summary,
    }

    matrix = [
        {
            "networkId": row["networkId"],
            "label": row["label"],
            "nodeCount": row["nodeCount"],
            "linkRadius": row["linkRadius"],
            "avgScore": row["avgScore"],
            "stableRatio": row["stableRatio"],
            "verdict": row["verdict"],
            "bestSeed": row["bestSeed"],
        }
        for row in recommendations
    ]

    network_details = []
    for network in (study.get("networks", []) or []):
        best = network.get("best", {}) or {}
        snapshots = best.get("snapshots", [])
        pass_metrics = build_compact_pass_metrics(snapshots)
        theorem_stats = _build_theorem_activation_stats(snapshots)
        network_details.append(
            {
                "id": network.get("id"),
                "label": network.get("label"),
                "nodeCount": network.get("nodeCount"),
                "linkRadius": network.get("linkRadius"),
                "bestAvgScore": float(best.get("avgScore", 0)),
                "bestStableRatio": float(best.get("stableRatio", 0)),
                "bestRunSeed": best.get("seed"),
                "bestRunVerdict": best.get("verdict"),
                "bestParameters": best.get("parameters", {}),
                "chartSeries": _build_chart_series(snapshots),
                "passMetrics": pass_metrics,
                "theoremActivation": theorem_stats,
                "scoreMetrics": best.get("scoreMetrics"),
                "scoreRationale": best.get("scoreRationale", []),
                "topology": best.get("topology"),
                "optimizationTrace": network.get("optimizationTrace", []),
            }
        )

    return {
        "metadata": metadata,
        "tunedParameters": tuned_parameters,
        "matrix": matrix,
        "recommendations": recommendations,
        "networkDetails": network_details,
        "dependencies": _build_dependency_data(runs, parameter_keys),
    }
