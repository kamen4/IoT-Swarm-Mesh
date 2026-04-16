"""Purpose: Deterministic seeded random generator compatible with JS xor-shift logic."""

from __future__ import annotations


def create_seeded_rng(seed: int):
    x = (int(seed) | 0) ^ 0x9E3779B9

    def next_value() -> float:
        nonlocal x
        x ^= (x << 13) & 0xFFFFFFFF
        x ^= (x >> 17) & 0xFFFFFFFF
        x ^= (x << 5) & 0xFFFFFFFF
        unsigned = x & 0xFFFFFFFF
        return unsigned / 4294967296.0

    return next_value


def random_in_range(rng, min_value: float, max_value: float) -> float:
    return min_value + (max_value - min_value) * rng()
