"""Purpose: Graph helpers for adjacency, BFS distances, and geometry operations."""

from __future__ import annotations

from collections import deque


def edge_key(a: int, b: int) -> str:
    return f"{a}:{b}" if a < b else f"{b}:{a}"


def distance(u: dict, v: dict) -> float:
    dx = float(u["x"]) - float(v["x"])
    dy = float(u["y"]) - float(v["y"])
    return (dx * dx + dy * dy) ** 0.5


def build_adjacency(node_count: int, edges: list[dict]) -> dict[int, set[int]]:
    adjacency: dict[int, set[int]] = {index: set() for index in range(node_count)}
    for edge in edges:
        adjacency[edge["a"]].add(edge["b"])
        adjacency[edge["b"]].add(edge["a"])
    return adjacency


def build_edge_lookup(edges: list[dict]) -> dict[str, dict]:
    lookup: dict[str, dict] = {}
    for edge in edges:
        lookup[edge_key(edge["a"], edge["b"])] = edge
    return lookup


def connected_components(adjacency: dict[int, set[int]]) -> list[list[int]]:
    seen: set[int] = set()
    components: list[list[int]] = []

    for node_id in adjacency:
        if node_id in seen:
            continue
        queue = deque([node_id])
        seen.add(node_id)
        component: list[int] = []

        while queue:
            current = queue.popleft()
            component.append(current)
            for nxt in adjacency[current]:
                if nxt not in seen:
                    seen.add(nxt)
                    queue.append(nxt)

        components.append(component)

    return components


def bfs_distances(adjacency: dict[int, set[int]], root_id: int) -> dict[int, float]:
    dist: dict[int, float] = {
        node_id: float("inf") for node_id in adjacency
    }
    dist[root_id] = 0.0
    queue = deque([root_id])

    while queue:
        current = queue.popleft()
        current_dist = dist[current]
        for nxt in adjacency[current]:
            if dist[nxt] == float("inf"):
                dist[nxt] = current_dist + 1.0
                queue.append(nxt)

    return dist
