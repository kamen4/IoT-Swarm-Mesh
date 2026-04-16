"""Purpose: Simulate one UP phase where devices try to route greedily toward gateway."""

from __future__ import annotations

from src.core.constants import THEOREM_ROOT_ID
from src.propagation.link_usage_tracker import (
    get_link_delivery_probability,
    record_link_usage,
)
from src.utils.math_utils import js_round


def _update_neighbor_estimate(observer_id: int, sender_id: int, state: dict) -> bool:
    observer = state["nodes"].get(observer_id)
    sender = state["nodes"].get(sender_id)
    estimate_map = state["estimates"].get(observer_id)
    if observer is None or sender is None or estimate_map is None:
        return False

    previous = float(estimate_map.get(sender_id, 0.0))
    nxt = max(previous, float(sender.q_total))
    if nxt > previous:
        estimate_map[sender_id] = nxt
        return True
    return False


def _choose_up_next_hop(node_id: int, visited: set[int], state: dict) -> int | None:
    node = state["nodes"].get(node_id)
    if node is None:
        return None

    preferred_parent = state["parentMap"].get(node_id)
    if preferred_parent is None:
        preferred_parent = node.parent

    if preferred_parent is not None and preferred_parent not in visited:
        parent = state["nodes"].get(preferred_parent)
        if parent and float(parent.q_total) > float(node.q_total):
            return preferred_parent

    neighbors = state["adjacency"].get(node_id, set())
    best_id: int | None = None
    best_charge = float("-inf")

    for neighbor_id in sorted(neighbors):
        if neighbor_id in visited:
            continue
        neighbor = state["nodes"].get(neighbor_id)
        if neighbor is None:
            continue
        if float(neighbor.q_total) <= float(node.q_total):
            continue

        if (
            float(neighbor.q_total) > best_charge
            or (
                float(neighbor.q_total) == best_charge
                and (best_id is None or neighbor_id < best_id)
            )
        ):
            best_charge = float(neighbor.q_total)
            best_id = neighbor_id

    return best_id


def run_up_round_phase(state: dict) -> dict:
    hop_gain = max(
        1,
        js_round(max(1.0, float(state["config"]["chargeDropPerHop"])) * 0.03),
    )

    attempted = 0
    reached_gateway = 0
    hops = 0
    updates = 0

    node_ids = sorted(state["nodes"].keys())
    for source_id in node_ids:
        if source_id == THEOREM_ROOT_ID:
            continue

        attempted += 1
        current_id = source_id
        visited = {source_id}
        safety_limit = max(1, len(state["nodes"]))

        for _ in range(safety_limit):
            next_id = _choose_up_next_hop(current_id, visited, state)
            if next_id is None:
                break

            sender = state["nodes"].get(current_id)
            receiver = state["nodes"].get(next_id)
            if sender is None or receiver is None:
                break

            probability = get_link_delivery_probability(state, current_id, next_id)
            if state["rng"]() > probability:
                break

            record_link_usage(state, current_id, next_id, 1)

            sender_before = float(sender.q_total)
            receiver_before = float(receiver.q_total)
            sender.q_total += hop_gain
            receiver.q_total += hop_gain

            if float(sender.q_total) > sender_before or float(receiver.q_total) > receiver_before:
                updates += 1

            if _update_neighbor_estimate(next_id, current_id, state):
                updates += 1

            hops += 1
            current_id = next_id

            if current_id == THEOREM_ROOT_ID:
                reached_gateway += 1
                break

            if current_id in visited:
                break
            visited.add(current_id)

    return {
        "attempted": attempted,
        "reachedGateway": reached_gateway,
        "hops": hops,
        "updates": updates,
    }
