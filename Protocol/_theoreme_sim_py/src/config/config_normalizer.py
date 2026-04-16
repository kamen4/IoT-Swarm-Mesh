"""Purpose: Clamp and normalize simulation configuration with cross-parameter constraints."""

from __future__ import annotations

import math

from src.core.constants import DEFAULT_CONFIG
from src.utils.math_utils import clamp, js_round, round_to


def _to_int(value, min_value: int, max_value: int) -> int:
    try:
        numeric = float(value)
    except (TypeError, ValueError):
        return min_value

    if not math.isfinite(numeric):
        return min_value

    clamped = clamp(numeric, min_value, max_value)
    return js_round(clamped)


def _to_float(value, min_value: float, max_value: float, precision: int = 2) -> float:
    try:
        numeric = float(value)
    except (TypeError, ValueError):
        return float(min_value)

    if not math.isfinite(numeric):
        return float(min_value)

    clamped = clamp(numeric, min_value, max_value)
    return round_to(clamped, precision)


def normalize_config(config: dict | None) -> dict:
    source = {**DEFAULT_CONFIG, **(config or {})}

    normalized = {
        "nodeCount": _to_int(source.get("nodeCount"), 8, 220),
        "linkRadius": _to_int(source.get("linkRadius"), 40, 420),
        "qForward": _to_int(source.get("qForward"), 20, 1800),
        "deliveryProbability": _to_float(
            source.get("deliveryProbability"), 0.05, 1, 2
        ),
        "penaltyLambda": _to_int(source.get("penaltyLambda"), 0, 250),
        "switchHysteresis": _to_int(source.get("switchHysteresis"), 0, 260),
        "switchHysteresisRatio": _to_float(
            source.get("switchHysteresisRatio"), 0, 0.4, 2
        ),
        "rootSourceCharge": _to_int(source.get("rootSourceCharge"), 250, 3000),
        "chargeDropPerHop": _to_int(source.get("chargeDropPerHop"), 5, 420),
        "chargeSpreadFactor": _to_float(
            source.get("chargeSpreadFactor"), 0.02, 1, 2
        ),
        "seed": _to_int(source.get("seed"), 1, 999999),
        "roundsPerSecond": _to_int(source.get("roundsPerSecond"), 1, 60),
        "maxRounds": _to_int(source.get("maxRounds"), 20, 10000),
        "enforceTheoremAssumptions": bool(source.get("enforceTheoremAssumptions")),
        "decayIntervalSteps": _to_int(source.get("decayIntervalSteps"), 0, 2000),
        "decayPercent": _to_float(source.get("decayPercent"), 0, 0.8, 2),
        "linkMemory": _to_float(source.get("linkMemory"), 0.6, 0.999, 3),
        "linkLearningRate": _to_float(source.get("linkLearningRate"), 0.01, 2, 2),
        "linkBonusMax": _to_int(source.get("linkBonusMax"), 0, 240),
    }

    if normalized["maxRounds"] < normalized["roundsPerSecond"] * 3:
        normalized["maxRounds"] = normalized["roundsPerSecond"] * 3

    if normalized["qForward"] >= normalized["rootSourceCharge"]:
        normalized["qForward"] = max(
            20,
            int(math.floor(normalized["rootSourceCharge"] * 0.72)),
        )

    if normalized["chargeDropPerHop"] >= normalized["rootSourceCharge"]:
        normalized["chargeDropPerHop"] = max(
            5,
            int(math.floor(normalized["rootSourceCharge"] * 0.28)),
        )

    return normalized
