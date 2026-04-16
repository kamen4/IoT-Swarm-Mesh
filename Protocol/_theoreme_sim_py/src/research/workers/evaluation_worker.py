"""Purpose: Evaluate one seed-run candidate for batch optimization, optionally with artifacts."""

from __future__ import annotations

from src.config.config_normalizer import normalize_config
from src.research.parameter_search_space import summarize_optimization_parameters
from src.research.stability_scorer import score_stability
from src.state.no_ui_simulation_runner import run_no_ui_simulation


def _compact_topology(state: dict) -> dict:
    return {
        "nodes": [
            {
                "id": node.id,
                "x": float(node.x),
                "y": float(node.y),
                "isGateway": bool(node.is_gateway),
            }
            for node in state["nodes"].values()
        ],
        "edges": [[edge["a"], edge["b"]] for edge in (state.get("edges") or [])],
    }


def evaluate_seed_run(payload: dict) -> dict:
    config_template = payload["configTemplate"]
    topology = payload["topology"]
    seed = int(payload["seed"])
    rounds_per_check = int(payload["roundsPerCheck"])
    capture_artifacts = bool(payload.get("captureArtifacts", False))

    config = normalize_config(
        {
            **config_template,
            "nodeCount": topology["nodeCount"],
            "linkRadius": topology["linkRadius"],
            "seed": seed,
            "maxRounds": rounds_per_check,
        }
    )

    simulation = run_no_ui_simulation(config, rounds_per_check)
    scored = score_stability(simulation)
    metrics = scored.get("metrics", {}) or {}

    run_record = {
        "seed": seed,
        "score": float(scored["score"]),
        "verdict": scored["verdict"],
        "config": config,
    }

    if capture_artifacts:
        run_record["scoreMetrics"] = scored.get("metrics", {})
        run_record["scoreRationale"] = scored.get("rationale", [])
        run_record["snapshots"] = simulation.get("snapshots", [])
        run_record["topology"] = _compact_topology(simulation["state"])

    return {
        "seed": seed,
        "runRecord": run_record,
        "score": float(scored["score"]),
        "tailEligibleRatio": float(scored.get("metrics", {}).get("eligibleTailRatio", 0)),
        "flappingAvg": float(scored.get("metrics", {}).get("flappingAvg", 0)),
        "evaluationRun": {
            "seed": seed,
            "runId": f"{topology['id']}@{seed}",
            "networkId": topology["id"],
            "nodeCount": int(topology.get("nodeCount", config["nodeCount"])),
            "linkRadius": int(topology.get("linkRadius", config["linkRadius"])),
            "score": float(scored["score"]),
            "verdict": scored["verdict"],
            "params": summarize_optimization_parameters(config),
            "theoremPassRate": float(metrics.get("theoremPassRate", 0.0)),
            "assumptionsPassRate": float(metrics.get("assumptionsPassRate", 0.0)),
            "coverageAvg": float(metrics.get("coverageAvg", 0.0)),
            "duplicateDrop": float(metrics.get("duplicateDrop", 0.0)),
            "parentChangeAvg": float(metrics.get("parentChangeAvg", 0.0)),
            "flappingAvg": float(metrics.get("flappingAvg", 0.0)),
            "eligibleTailRatio": float(metrics.get("eligibleTailRatio", 0.0)),
        },
    }
