"""Purpose: Track edge usage and convert traffic concentration into stronger links."""

from __future__ import annotations

import math

from src.core.graph_utils import edge_key
from src.utils.math_utils import clamp


def create_link_stats(edges: list[dict]) -> dict[str, dict]:
    stats: dict[str, dict] = {}
    for edge in edges:
        key = edge_key(edge["a"], edge["b"])
        quality = clamp(float(edge.get("quality", 0.1)), 0.05, 1)
        stats[key] = {
            "baseQuality": quality,
            "effectiveQuality": quality,
            "usageScore": 0.0,
            "roundUsage": 0.0,
            "totalUsage": 0.0,
        }
    return stats


def record_link_usage(state: dict, a: int, b: int, amount: float = 1.0) -> None:
    stat = state.get("linkStats", {}).get(edge_key(a, b))
    if not stat:
        return
    increment = max(0.0, float(amount or 0.0))
    stat["roundUsage"] += increment
    stat["totalUsage"] += increment


def get_effective_link_quality(state: dict, a: int, b: int) -> float:
    key = edge_key(a, b)
    stat = state.get("linkStats", {}).get(key)
    if stat:
        return float(clamp(float(stat["effectiveQuality"]), 0.05, 1))

    edge = state.get("edgeLookup", {}).get(key)
    if not edge:
        return 0.1
    return float(clamp(float(edge.get("quality", 0.1)), 0.05, 1))


def get_link_delivery_probability(state: dict, a: int, b: int) -> float:
    quality = get_effective_link_quality(state, a, b)
    base = float(state["config"].get("deliveryProbability", 0.5))
    probability = 0.04 + base * (0.2 + 0.8 * quality)
    return float(clamp(probability, 0.02, 1))


def finalize_link_strength_round(state: dict) -> None:
    link_stats: dict[str, dict] = state.get("linkStats", {})
    if not link_stats:
        return

    memory = clamp(float(state["config"].get("linkMemory", 0.9)), 0.6, 0.999)
    learning = clamp(float(state["config"].get("linkLearningRate", 0.2)), 0.01, 2)

    for stat in link_stats.values():
        stat["usageScore"] = stat["usageScore"] * memory + stat["roundUsage"]
        boost = 1 - math.exp(-stat["usageScore"] * 0.035 * learning)

        target_quality = clamp(
            float(stat["baseQuality"]) + (1 - float(stat["baseQuality"])) * boost,
            0.05,
            1,
        )

        stat["effectiveQuality"] = (
            float(stat["effectiveQuality"]) * (1 - learning * 0.12)
            + target_quality * (learning * 0.12)
        )
        stat["effectiveQuality"] = float(clamp(stat["effectiveQuality"], 0.05, 1))
        stat["roundUsage"] = 0.0


def decay_link_stats(state: dict, factor: float) -> None:
    link_stats: dict[str, dict] = state.get("linkStats", {})
    if not link_stats:
        return

    clamped_factor = clamp(float(factor or 1), 0, 1)

    for stat in link_stats.values():
        stat["usageScore"] *= clamped_factor
        stat["effectiveQuality"] = float(
            clamp(
                float(stat["baseQuality"])
                + (float(stat["effectiveQuality"]) - float(stat["baseQuality"])) * clamped_factor,
                0.05,
                1,
            )
        )


def compute_link_stability_bonus(state: dict, parent_id: int, child_id: int) -> float:
    stat = state.get("linkStats", {}).get(edge_key(parent_id, child_id))
    if not stat:
        return 0.0

    bonus_max = max(0.0, float(state["config"].get("linkBonusMax", 0)))
    if bonus_max <= 0:
        return 0.0

    normalized = 1 - math.exp(-float(stat["usageScore"]) * 0.02)
    return bonus_max * normalized
