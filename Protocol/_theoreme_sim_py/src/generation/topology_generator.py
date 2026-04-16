"""Purpose: Generate connected geometric topology used for theorem simulation rounds."""

from __future__ import annotations

from src.core.constants import LAYOUT
from src.core.graph_utils import (
    build_adjacency,
    connected_components,
    distance,
    edge_key,
)
from src.core.random import random_in_range


def generate_connected_topology(node_count: int, link_radius: float, rng) -> dict:
    nodes: list[dict] = []

    for node_id in range(node_count):
        nodes.append(
            {
                "id": node_id,
                "x": random_in_range(
                    rng,
                    float(LAYOUT["margin"]),
                    float(LAYOUT["width"] - LAYOUT["margin"]),
                ),
                "y": random_in_range(
                    rng,
                    float(LAYOUT["margin"]),
                    float(LAYOUT["height"] - LAYOUT["margin"]),
                ),
            }
        )

    edges: list[dict] = []
    existing: set[str] = set()

    def add_edge(a: int, b: int) -> None:
        if a == b:
            return

        key = edge_key(a, b)
        if key in existing:
            return

        d = distance(nodes[a], nodes[b])
        quality = max(0.08, 1 - d / (float(link_radius) * 1.35))
        edges.append(
            {
                "a": min(a, b),
                "b": max(a, b),
                "distance": d,
                "quality": quality,
            }
        )
        existing.add(key)

    for i in range(node_count):
        for j in range(i + 1, node_count):
            if distance(nodes[i], nodes[j]) <= link_radius:
                add_edge(i, j)

    adjacency = build_adjacency(node_count, edges)

    # Ensure every node has at least one neighbor.
    for i in range(node_count):
        if adjacency[i]:
            continue

        nearest = -1
        nearest_distance = float("inf")
        for j in range(node_count):
            if i == j:
                continue
            d = distance(nodes[i], nodes[j])
            if d < nearest_distance:
                nearest_distance = d
                nearest = j

        if nearest >= 0:
            add_edge(i, nearest)

    adjacency = build_adjacency(node_count, edges)

    # Merge components by shortest cross-edge.
    components = connected_components(adjacency)
    while len(components) > 1:
        primary = components[0]
        secondary = components[1]

        best_a = primary[0]
        best_b = secondary[0]
        best_distance = float("inf")

        for a in primary:
            for b in secondary:
                d = distance(nodes[a], nodes[b])
                if d < best_distance:
                    best_distance = d
                    best_a = a
                    best_b = b

        add_edge(best_a, best_b)
        adjacency = build_adjacency(node_count, edges)
        components = connected_components(adjacency)

    return {
        "nodes": nodes,
        "edges": edges,
        "adjacency": adjacency,
    }
