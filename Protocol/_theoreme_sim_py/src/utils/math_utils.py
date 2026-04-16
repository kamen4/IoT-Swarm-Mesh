"""Purpose: JS-compatible rounding and numeric helpers for deterministic parity."""

from __future__ import annotations

import math


def clamp(value: float, min_value: float, max_value: float) -> float:
    return min(max_value, max(min_value, value))


def js_round(value: float) -> int:
    """Replicate Math.round behavior for finite numbers."""
    if not math.isfinite(value):
        return 0
    if value >= 0:
        return int(math.floor(value + 0.5))
    return int(math.ceil(value - 0.5))


def round_to(value: float, precision: int) -> float:
    factor = 10**precision
    return js_round(value * factor) / factor
