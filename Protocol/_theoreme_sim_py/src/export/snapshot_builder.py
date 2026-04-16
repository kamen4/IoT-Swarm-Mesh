"""Purpose: Build round and final snapshots for JSON/CSV-friendly outputs."""

from __future__ import annotations

from datetime import datetime, timezone


def build_round_snapshot(state: dict) -> dict:
    report = state.get("lastTheoremReport", {})
    broadcast = state.get("lastBroadcastReport", {})
    up = state.get(
        "lastUp",
        {"attempted": 0, "reachedGateway": 0, "hops": 0, "updates": 0},
    )
    decay = state.get(
        "lastDecay",
        {"triggered": False, "epoch": 0, "percent": 0, "factor": 1},
    )
    oscillation = state.get(
        "lastOscillationReport",
        {
            "changedParents": 0,
            "flappingNodes": 0,
            "totalTracked": 0,
            "maxFlips": 0,
        },
    )

    return {
        "round": state["round"],
        "verificationState": report.get("verificationState", "pending"),
        "eligibleCount": report.get("eligibleCount", 0),
        "assumptionsPass": report.get("assumptionsPass"),
        "theoremPass": report.get("theoremPass"),
        "a5": report.get("a5"),
        "a6": report.get("a6"),
        "a7": report.get("a7"),
        "lemma41": report.get("lemma41"),
        "lemma42": report.get("lemma42"),
        "lemma43": report.get("lemma43"),
        "stableRounds": state.get("stableRounds", 0),
        "propagationUpdates": state.get("lastPropagation", {}).get("updates", 0),
        "propagationDeliveries": state.get("lastPropagation", {}).get("deliveries", 0),
        "downDuplicates": broadcast.get("duplicates", 0),
        "downReachedCount": broadcast.get("reachedCount", 0),
        "downCoverage": broadcast.get("coverage", 0),
        "upAttempted": up.get("attempted", 0),
        "upReachedGateway": up.get("reachedGateway", 0),
        "upHops": up.get("hops", 0),
        "upUpdates": up.get("updates", 0),
        "decayTriggered": decay.get("triggered", False),
        "decayEpoch": decay.get("epoch", 0),
        "decayPercent": decay.get("percent", 0),
        "parentChanges": oscillation.get("changedParents", 0),
        "flappingNodes": oscillation.get("flappingNodes", 0),
        "maxFlips": oscillation.get("maxFlips", 0),
        "spreadUpdates": state.get("lastSpread", {}).get("updates", 0),
    }


def build_final_snapshot(state: dict, snapshots: list[dict], steps_executed: int) -> dict:
    exported_at = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")

    return {
        "exportedAt": exported_at,
        "stepsExecuted": steps_executed,
        "config": state["config"],
        "finalRound": state["round"],
        "theorem": state.get("lastTheoremReport"),
        "broadcast": state.get("lastBroadcastReport"),
        "broadcastHistory": state.get("broadcastHistory", []),
        "decayHistory": state.get("decayHistory", []),
        "oscillation": state.get("lastOscillationReport"),
        "up": state.get("lastUp"),
        "nodes": [
            {
                "id": node.id,
                "qTotal": node.q_total,
                "eligible": node.eligible,
                "parent": node.parent,
                "children": node.children,
            }
            for node in state["nodes"].values()
        ],
        "snapshots": snapshots,
    }
