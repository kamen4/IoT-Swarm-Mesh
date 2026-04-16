"""Purpose: Asynchronous-like neighbor charge learning with max-based estimate updates."""

from __future__ import annotations

from src.propagation.link_usage_tracker import (
    get_link_delivery_probability,
    record_link_usage,
)


def propagate_neighbor_charges_round(state: dict) -> dict:
    updates = 0
    deliveries = 0

    def try_deliver(observer_id: int, sender_id: int) -> None:
        nonlocal updates, deliveries

        probability = get_link_delivery_probability(state, observer_id, sender_id)
        if state["rng"]() > probability:
            return

        record_link_usage(state, observer_id, sender_id, 0.35)
        deliveries += 1

        estimate_map: dict[int, float] = state["estimates"].get(observer_id, {})
        old_value = float(estimate_map.get(sender_id, 0.0))
        advertised_charge = float(state["nodes"][sender_id].q_total)
        next_value = max(old_value, advertised_charge)

        if next_value > old_value:
            updates += 1
            estimate_map[sender_id] = next_value

    for edge in state["edges"]:
        try_deliver(edge["a"], edge["b"])
        try_deliver(edge["b"], edge["a"])

    return {"updates": updates, "deliveries": deliveries}
