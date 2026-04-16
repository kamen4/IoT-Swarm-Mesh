"""Purpose: Verify theorem assumptions A5, A6, and A7 on current simulation state."""

from __future__ import annotations

from src.core.constants import THEOREM_ROOT_ID


def get_eligible_node_ids(state: dict) -> list[int]:
    return sorted([node.id for node in state["nodes"].values() if node.eligible])


def check_a5_unique_gateway_maximum(state: dict) -> dict:
    eligible = get_eligible_node_ids(state)
    root = state["nodes"].get(THEOREM_ROOT_ID)

    if root is None or not root.eligible:
        return {"pass": False, "violations": eligible}

    violations: list[int] = []
    for node_id in eligible:
        if node_id == THEOREM_ROOT_ID:
            continue
        if not (float(root.q_total) > float(state["nodes"][node_id].q_total)):
            violations.append(node_id)

    return {"pass": len(violations) == 0, "violations": violations}


def check_a6_local_progress(state: dict) -> dict:
    violations: list[int] = []

    for node in state["nodes"].values():
        if not node.eligible or node.is_gateway:
            continue

        neighbors = state["adjacency"].get(node.id, set())
        has_higher_eligible_neighbor = False

        for neighbor_id in neighbors:
            neighbor = state["nodes"].get(neighbor_id)
            if neighbor and neighbor.eligible and float(neighbor.q_total) > float(node.q_total):
                has_higher_eligible_neighbor = True
                break

        if not has_higher_eligible_neighbor:
            violations.append(node.id)

    return {"pass": len(violations) == 0, "violations": violations}


def check_a7_parent_rule(state: dict) -> dict:
    violations: list[int] = []

    for node in state["nodes"].values():
        if not node.eligible or node.is_gateway:
            continue

        if node.parent is None:
            violations.append(node.id)
            continue

        neighbors = state["adjacency"].get(node.id, set())
        if node.parent not in neighbors:
            violations.append(node.id)
            continue

        parent = state["nodes"].get(node.parent)
        if parent is None or (not parent.eligible) or float(parent.q_total) <= float(node.q_total):
            violations.append(node.id)

    return {"pass": len(violations) == 0, "violations": violations}


def check_assumptions(state: dict) -> dict:
    a5 = check_a5_unique_gateway_maximum(state)
    a6 = check_a6_local_progress(state)
    a7 = check_a7_parent_rule(state)
    return {"a5": a5, "a6": a6, "a7": a7}
