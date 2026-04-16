"""Purpose: Rebuild parent map and derived children map from current estimates."""

from __future__ import annotations

from src.routing.parent_selection import choose_parent


def rebuild_tree(state: dict) -> dict:
    next_parent_map: dict[int, int | None] = {}

    for node in state["nodes"].values():
        if node.is_gateway or not node.eligible:
            next_parent_map[node.id] = None
            continue
        next_parent_map[node.id] = choose_parent(node.id, state)

    changed_count = 0
    for node in state["nodes"].values():
        prev = state["parentMap"].get(node.id)
        nxt = next_parent_map.get(node.id)
        if prev != nxt:
            changed_count += 1
        node.parent = nxt

    children_map: dict[int, set[int]] = {
        node.id: set() for node in state["nodes"].values()
    }

    for child_id, parent_id in next_parent_map.items():
        if parent_id is not None and parent_id in children_map:
            children_map[parent_id].add(child_id)

    for node in state["nodes"].values():
        node.children = sorted(children_map.get(node.id, set()))

    return {
        "changedCount": changed_count,
        "parentMap": next_parent_map,
        "childrenMap": children_map,
    }
