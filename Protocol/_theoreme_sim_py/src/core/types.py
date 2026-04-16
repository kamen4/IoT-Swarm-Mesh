"""Purpose: Data contracts and constructors for simulation state structures."""

from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(slots=True)
class SimNode:
    id: int
    x: float
    y: float
    q_total: float
    is_gateway: bool
    eligible: bool
    parent: int | None
    children: list[int] = field(default_factory=list)


@dataclass(slots=True)
class SimEdge:
    a: int
    b: int
    distance: float
    quality: float


def map_nodes_by_id(nodes: list[SimNode]) -> dict[int, SimNode]:
    return {node.id: node for node in nodes}


def create_estimate_map(adjacency: dict[int, set[int]]) -> dict[int, dict[int, float]]:
    outer: dict[int, dict[int, float]] = {}
    for node_id, neighbors in adjacency.items():
        outer[node_id] = {neighbor_id: 0.0 for neighbor_id in neighbors}
    return outer


def create_empty_broadcast_report() -> dict:
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


def create_pending_theorem_report() -> dict:
    return {
        "assumptionsPass": None,
        "theoremPass": None,
        "eligibleCount": 0,
        "a5": None,
        "a6": None,
        "a7": None,
        "lemma41": None,
        "lemma42": None,
        "lemma43": None,
        "violationsA6": [],
        "unreachable": [],
        "cycleWitness": [],
        "verificationState": "pending",
    }
