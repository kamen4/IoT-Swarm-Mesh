"""Purpose: Apply optional global decay to charges and neighbor estimate tables."""

from __future__ import annotations

from src.utils.math_utils import clamp


def apply_global_decay(nodes: dict, estimates: dict[int, dict[int, float]], factor: float) -> None:
    clamped = clamp(float(factor), 0.01, 1)

    for node in nodes.values():
        node.q_total *= clamped

    for neighbor_map in estimates.values():
        for neighbor_id, value in list(neighbor_map.items()):
            neighbor_map[neighbor_id] = float(value) * clamped
