"""Purpose: Initialize near-empty charge field so dynamics grow during simulation rounds."""

from __future__ import annotations

from src.core.constants import THEOREM_ROOT_ID
from src.core.graph_utils import bfs_distances
from src.core.types import SimNode


def initialize_charges(
    nodes: list[dict],
    adjacency: dict[int, set[int]],
    config: dict,
    _rng,
) -> dict:
    distances = bfs_distances(adjacency, THEOREM_ROOT_ID)

    initialized: list[SimNode] = []
    for node in nodes:
        q_total = float(config["rootSourceCharge"]) if node["id"] == THEOREM_ROOT_ID else 0.0
        initialized.append(
            SimNode(
                id=node["id"],
                x=float(node["x"]),
                y=float(node["y"]),
                q_total=q_total,
                is_gateway=node["id"] == THEOREM_ROOT_ID,
                eligible=False,
                parent=None,
            )
        )

    q_forward = float(config["qForward"])
    for node in initialized:
        node.eligible = node.q_total >= q_forward

    return {
        "nodes": initialized,
        "distances": distances,
    }
