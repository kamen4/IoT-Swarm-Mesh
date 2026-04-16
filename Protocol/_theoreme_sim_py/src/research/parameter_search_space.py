"""Purpose: Adaptive parameter-space encoding and vector helpers for batch optimization."""

from __future__ import annotations

from src.config.config_normalizer import normalize_config
from src.config.parameter_ranges import PARAMETER_RANGES
from src.utils.math_utils import clamp, js_round, round_to

OPTIMIZATION_KEYS = [
    "qForward",
    "deliveryProbability",
    "rootSourceCharge",
    "penaltyLambda",
    "switchHysteresis",
    "switchHysteresisRatio",
    "chargeDropPerHop",
    "chargeSpreadFactor",
    "decayIntervalSteps",
    "decayPercent",
    "linkMemory",
    "linkLearningRate",
    "linkBonusMax",
]

OPTIMIZATION_PARAMETERS = [
    {
        "key": key,
        "label": key,
        "min": float(PARAMETER_RANGES[key]["min"]),
        "max": float(PARAMETER_RANGES[key]["max"]),
        "type": PARAMETER_RANGES[key]["type"],
        "precision": int(PARAMETER_RANGES[key].get("precision", 0)),
    }
    for key in OPTIMIZATION_KEYS
]


def _clamp_unit(value: float) -> float:
    return float(clamp(float(value), 0, 1))


def _to_unit(value: float, param: dict) -> float:
    span = max(1e-9, float(param["max"]) - float(param["min"]))
    return _clamp_unit((float(value) - float(param["min"])) / span)


def _from_unit(unit: float, param: dict) -> float:
    span = max(1e-9, float(param["max"]) - float(param["min"]))
    raw = float(param["min"]) + _clamp_unit(unit) * span

    if param["type"] == "int":
        return float(js_round(raw))
    return float(round_to(raw, int(param.get("precision", 0))))


def clamp_vector(vector: list[float] | None) -> list[float]:
    return [_clamp_unit(value) for value in (vector or [])]


def encode_optimization_vector(config: dict | None) -> list[float]:
    normalized = normalize_config(config or {})
    return [
        _to_unit(float(normalized[param["key"]]), param)
        for param in OPTIMIZATION_PARAMETERS
    ]


def decode_optimization_vector(vector: list[float], base_config: dict | None) -> dict:
    nxt = dict(base_config or {})
    unit_vector = clamp_vector(vector)

    for index, param in enumerate(OPTIMIZATION_PARAMETERS):
        value = _from_unit(unit_vector[index], param)
        if param["type"] == "int":
            nxt[param["key"]] = int(value)
        else:
            nxt[param["key"]] = float(value)

    return normalize_config(nxt)


def random_direction(rng, active_dimensions: int = 3) -> list[int]:
    dimension_count = len(OPTIMIZATION_PARAMETERS)
    active = max(1, min(dimension_count, int(active_dimensions or 1)))

    direction = [0] * dimension_count
    selected: set[int] = set()

    while len(selected) < active:
        selected.add(int(rng() * dimension_count))

    for index in selected:
        direction[index] = 1 if rng() >= 0.5 else -1

    return direction


def summarize_optimization_parameters(config: dict | None) -> dict[str, float]:
    normalized = normalize_config(config or {})
    summary: dict[str, float] = {}

    for param in OPTIMIZATION_PARAMETERS:
        summary[param["key"]] = float(normalized[param["key"]])

    return summary
