"""Purpose: Per-round node charge update based on best learned neighbor estimate."""

from __future__ import annotations

from src.core.constants import THEOREM_ROOT_ID
from src.utils.math_utils import clamp


def apply_charge_spread_round(state: dict) -> dict:
    next_values: dict[int, float] = {}

    for node in state["nodes"].values():
        if node.id == THEOREM_ROOT_ID or node.is_gateway:
            next_values[node.id] = max(
                float(node.q_total),
                float(state["config"]["rootSourceCharge"]),
            )
            continue

        estimate_map = state["estimates"].get(node.id, {})
        best_estimate = 0.0
        for estimate in estimate_map.values():
            best_estimate = max(best_estimate, float(estimate))

        target = max(0.0, best_estimate - float(state["config"]["chargeDropPerHop"]))
        factor = clamp(float(state["config"]["chargeSpreadFactor"]), 0.01, 1)
        blended = float(node.q_total) + (target - float(node.q_total)) * factor
        next_values[node.id] = max(float(node.q_total), blended)

    updates = 0
    for node in state["nodes"].values():
        next_charge = next_values[node.id]
        if next_charge > float(node.q_total):
            updates += 1
        node.q_total = next_charge

    return {"updates": updates}
