"""Purpose: Select strict higher-charge eligible parent with hysteresis and link bonus."""

from __future__ import annotations

from src.propagation.link_usage_tracker import (
    compute_link_stability_bonus,
    get_effective_link_quality,
)


def choose_parent(node_id: int, state: dict) -> int | None:
    node = state["nodes"].get(node_id)
    if node is None or node.is_gateway or not node.eligible:
        return None

    neighbors = state["adjacency"].get(node_id, set())
    estimate_map = state["estimates"].get(node_id, {})
    candidates: list[dict] = []

    for neighbor_id in sorted(neighbors):
        neighbor = state["nodes"].get(neighbor_id)
        if neighbor is None or not neighbor.eligible:
            continue

        if float(neighbor.q_total) <= float(node.q_total):
            continue

        estimate = float(estimate_map.get(neighbor_id, 0.0))
        if estimate <= float(node.q_total) or estimate < float(state["config"]["qForward"]):
            continue

        quality = get_effective_link_quality(state, node_id, neighbor_id)
        penalty = (1 - quality) * float(state["config"]["penaltyLambda"])
        stability_bonus = compute_link_stability_bonus(state, neighbor_id, node_id)
        candidates.append(
            {
                "id": neighbor_id,
                "penalty": penalty,
                "estimate": estimate,
                "stabilityBonus": stability_bonus,
            }
        )

    if not candidates:
        return None

    max_estimate = max(candidate["estimate"] for candidate in candidates)
    tie_window = max(1.0, max_estimate * 0.02)

    near_best = [
        candidate
        for candidate in candidates
        if max_estimate - candidate["estimate"] <= tie_window
    ]

    for candidate in near_best:
        candidate["score"] = (
            candidate["estimate"] - candidate["penalty"] + candidate["stabilityBonus"]
        )

    near_best.sort(
        key=lambda item: (-item["score"], -item["estimate"], item["id"])
    )

    best = near_best[0]
    current_parent = node.parent

    if current_parent is None:
        return int(best["id"])

    current = next(
        (candidate for candidate in near_best if candidate["id"] == current_parent),
        None,
    )

    if current is None:
        current = next(
            (candidate for candidate in candidates if candidate["id"] == current_parent),
            None,
        )
        if current is not None:
            current["score"] = (
                current["estimate"]
                - current["penalty"]
                + current["stabilityBonus"]
            )

    if current is None:
        return int(best["id"])

    ratio = max(0.0, float(state["config"].get("switchHysteresisRatio", 0.0)))
    relative_margin = max(0.0, current["estimate"] * ratio)
    required_margin = max(float(state["config"]["switchHysteresis"]), relative_margin)

    keep_current = current["score"] + required_margin >= best["score"]
    return int(current_parent if keep_current else best["id"])
