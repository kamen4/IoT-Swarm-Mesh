"""Purpose: Simulate one DOWN phase from gateway and track duplicates and coverage."""

from __future__ import annotations

from src.core.constants import THEOREM_ROOT_ID
from src.propagation.link_usage_tracker import record_link_usage
from src.utils.math_utils import clamp


def _update_neighbor_estimate(
    observer_id: int,
    sender_id: int,
    advertised_charge: float,
    state: dict,
) -> bool:
    estimate_map = state["estimates"].get(observer_id)
    if estimate_map is None:
        return False

    previous = float(estimate_map.get(sender_id, 0.0))
    nxt = max(previous, float(advertised_charge))
    if nxt > previous:
        estimate_map[sender_id] = nxt
        return True
    return False


def _select_forward_targets(node_id: int, from_id: int | None, state: dict) -> list[int]:
    neighbors = state["adjacency"].get(node_id, set())

    if node_id == THEOREM_ROOT_ID:
        return list(neighbors)

    node = state["nodes"].get(node_id)
    if not node:
        return []

    targets: set[int] = set()

    if node.eligible:
        children = state["childrenMap"].get(node_id, set())
        for child_id in children:
            child = state["nodes"].get(child_id)
            if child and child.eligible:
                targets.add(child_id)

        # Keep warming still-cold nodes while the tree matures.
        for neighbor_id in neighbors:
            if neighbor_id == from_id:
                continue
            neighbor = state["nodes"].get(neighbor_id)
            if neighbor and not neighbor.eligible:
                targets.add(neighbor_id)
        return list(targets)

    for neighbor_id in neighbors:
        if neighbor_id != from_id:
            targets.add(neighbor_id)

    return list(targets)


def run_down_round_phase(state: dict) -> dict:
    root = state["nodes"].get(THEOREM_ROOT_ID)
    if root is None:
        return {
            "order": [],
            "duplicates": 0,
            "loopDetected": False,
            "reachedCount": 0,
            "coverage": 0.0,
            "deliveries": 0,
            "propagationUpdates": 0,
            "spreadUpdates": 0,
            "mode": "hybrid",
        }

    root.q_total = max(float(root.q_total), float(state["config"]["rootSourceCharge"]))

    spread_factor = clamp(float(state["config"].get("chargeSpreadFactor", 0.0)), 0.01, 1)
    transmissions: list[dict] = []
    received: set[int] = {THEOREM_ROOT_ID}
    order = [THEOREM_ROOT_ID]

    duplicates = 0
    deliveries = 1
    propagation_updates = 0
    spread_updates = 0

    for target_id in _select_forward_targets(THEOREM_ROOT_ID, None, state):
        transmissions.append(
            {
                "fromId": THEOREM_ROOT_ID,
                "toId": target_id,
                "advertisedCharge": float(root.q_total),
            }
        )

    while transmissions:
        tx = transmissions.pop(0)
        receiver = state["nodes"].get(tx["toId"])
        if receiver is None:
            continue

        record_link_usage(state, tx["fromId"], tx["toId"], 1)

        if tx["toId"] in received:
            duplicates += 1
            continue

        received.add(tx["toId"])
        order.append(tx["toId"])
        deliveries += 1

        if _update_neighbor_estimate(
            tx["toId"], tx["fromId"], tx["advertisedCharge"], state
        ):
            propagation_updates += 1

        before = float(receiver.q_total)
        target_charge = max(
            0.0,
            float(tx["advertisedCharge"]) - float(state["config"]["chargeDropPerHop"]),
        )
        blended = before + (target_charge - before) * spread_factor
        next_charge = max(before, blended)

        if next_charge > before:
            spread_updates += 1

        receiver.q_total = next_charge

        for target_id in _select_forward_targets(tx["toId"], tx["fromId"], state):
            transmissions.append(
                {
                    "fromId": tx["toId"],
                    "toId": target_id,
                    "advertisedCharge": float(receiver.q_total),
                }
            )

    reached_count = len(received)
    coverage = (
        reached_count / max(1, len(state["nodes"])) if state["nodes"] else 0.0
    )

    return {
        "order": order,
        "duplicates": duplicates,
        "loopDetected": duplicates > 0,
        "reachedCount": reached_count,
        "coverage": coverage,
        "deliveries": deliveries,
        "propagationUpdates": propagation_updates,
        "spreadUpdates": spread_updates,
        "mode": "hybrid",
    }
