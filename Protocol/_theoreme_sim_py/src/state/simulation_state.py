"""Purpose: Build initial state with topology, charges, estimates, and metadata."""

from __future__ import annotations

from src.config.config_normalizer import normalize_config
from src.core.graph_utils import build_edge_lookup
from src.core.random import create_seeded_rng
from src.core.types import (
    create_empty_broadcast_report,
    create_estimate_map,
    create_pending_theorem_report,
    map_nodes_by_id,
)
from src.generation.charge_initializer import initialize_charges
from src.generation.topology_generator import generate_connected_topology
from src.propagation.link_usage_tracker import create_link_stats


def _charge_bounds_from_nodes(nodes: dict) -> dict:
    min_charge = float("inf")
    max_charge = float("-inf")

    for node in nodes.values():
        min_charge = min(min_charge, float(node.q_total))
        max_charge = max(max_charge, float(node.q_total))

    if min_charge == float("inf") or max_charge == float("-inf"):
        return {"minCharge": 0, "maxCharge": 1}

    return {"minCharge": min_charge, "maxCharge": max_charge}


def refresh_eligibility(nodes: dict, q_forward: float) -> None:
    for node in nodes.values():
        node.eligible = float(node.q_total) >= float(q_forward)


def create_simulation_state(config: dict | None) -> dict:
    normalized_config = normalize_config(config)
    rng = create_seeded_rng(int(normalized_config["seed"]))

    topology = generate_connected_topology(
        int(normalized_config["nodeCount"]),
        float(normalized_config["linkRadius"]),
        rng,
    )

    charged = initialize_charges(
        topology["nodes"],
        topology["adjacency"],
        normalized_config,
        rng,
    )

    nodes = map_nodes_by_id(charged["nodes"])
    estimates = create_estimate_map(topology["adjacency"])

    parent_map: dict[int, int | None] = {}
    children_map: dict[int, set[int]] = {}
    for node in nodes.values():
        parent_map[node.id] = None
        children_map[node.id] = set()

    return {
        "config": normalized_config,
        "rng": rng,
        "nodes": nodes,
        "edges": topology["edges"],
        "adjacency": topology["adjacency"],
        "edgeLookup": build_edge_lookup(topology["edges"]),
        "linkStats": create_link_stats(topology["edges"]),
        "estimates": estimates,
        "distances": charged["distances"],
        "parentMap": parent_map,
        "childrenMap": children_map,
        "round": 0,
        "stableRounds": 0,
        "decayEpoch": 0,
        "decayHistory": [],
        "lastDecay": {
            "triggered": False,
            "epoch": 0,
            "percent": 0,
            "factor": 1,
        },
        "parentTrace": {},
        "lastOscillationReport": {
            "changedParents": 0,
            "flappingNodes": 0,
            "totalTracked": 0,
            "maxFlips": 0,
        },
        "lastPropagation": {"updates": 0, "deliveries": 0},
        "lastUp": {"attempted": 0, "reachedGateway": 0, "hops": 0, "updates": 0},
        "lastSpread": {"updates": 0},
        "lastTheoremReport": create_pending_theorem_report(),
        "lastBroadcastReport": create_empty_broadcast_report(),
        "broadcastHistory": [],
        "chargeBounds": _charge_bounds_from_nodes(nodes),
    }


def refresh_charge_bounds(state: dict) -> None:
    state["chargeBounds"] = _charge_bounds_from_nodes(state["nodes"])
