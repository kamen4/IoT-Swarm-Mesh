"""Purpose: Track parent flapping metrics over rolling history window."""

from __future__ import annotations

from src.core.constants import THEOREM_ROOT_ID

HISTORY_WINDOW = 12


def _count_flips(history: list[int | None]) -> int:
    flips = 0
    for index in range(1, len(history)):
        prev = history[index - 1]
        nxt = history[index]
        if prev is not None and nxt is not None and prev != nxt:
            flips += 1
    return flips


def update_oscillation_report(state: dict) -> dict:
    if not isinstance(state.get("parentTrace"), dict):
        state["parentTrace"] = {}

    changed_parents = 0
    flapping_nodes = 0
    max_flips = 0
    total_tracked = 0

    for node_id, parent_id in state["parentMap"].items():
        if node_id == THEOREM_ROOT_ID:
            continue

        total_tracked += 1

        trace = state["parentTrace"].get(
            node_id,
            {
                "history": [],
                "flips": 0,
                "lastParent": None,
            },
        )

        if trace["lastParent"] is not None and trace["lastParent"] != parent_id:
            trace["flips"] += 1
            changed_parents += 1

        trace["lastParent"] = parent_id
        trace["history"].append(parent_id)

        if len(trace["history"]) > HISTORY_WINDOW:
            trace["history"].pop(0)

        unique = {item for item in trace["history"] if item is not None}
        local_flips = _count_flips(trace["history"])
        if len(unique) >= 2 and local_flips >= 3:
            flapping_nodes += 1

        max_flips = max(max_flips, int(trace["flips"]))
        state["parentTrace"][node_id] = trace

    return {
        "changedParents": changed_parents,
        "flappingNodes": flapping_nodes,
        "totalTracked": total_tracked,
        "maxFlips": max_flips,
    }
