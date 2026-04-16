"""Purpose: Candidate preference logic and adaptive step-size heuristics."""

from __future__ import annotations


def _objective_score(
    score: float,
    stable_ratio: float,
    tail_eligible_ratio: float,
    flapping_avg: float,
    score_std_dev: float,
) -> float:
    score_component = float(score or 0)
    stable_component = float(stable_ratio or 0) * 26
    tail_health_component = float(tail_eligible_ratio or 0) * 12
    flapping_penalty = max(0.0, float(flapping_avg or 0) - 0.6) * 2.4
    variance_penalty = float(score_std_dev or 0) * 0.85

    return (
        score_component
        + stable_component
        + tail_health_component
        - flapping_penalty
        - variance_penalty
    )


def create_optimization_candidate(input_data: dict | None) -> dict:
    source = input_data or {}
    score = float(source.get("score", 0))
    stable_ratio = float(source.get("stableRatio", 0))
    tail_eligible_ratio = float(source.get("tailEligibleRatio", 0))
    flapping_avg = float(source.get("flappingAvg", 0))
    score_std_dev = float(source.get("scoreStdDev", 0))

    return {
        "mode": source.get("mode", "hold"),
        "vector": source.get("vector", []),
        "score": score,
        "stableRatio": stable_ratio,
        "tailEligibleRatio": tail_eligible_ratio,
        "flappingAvg": flapping_avg,
        "scoreStdDev": score_std_dev,
        "objective": _objective_score(
            score,
            stable_ratio,
            tail_eligible_ratio,
            flapping_avg,
            score_std_dev,
        ),
    }


def optimization_preference(left: dict, right: dict) -> float:
    objective_delta = float(left.get("objective", 0)) - float(right.get("objective", 0))
    if abs(objective_delta) > 1e-9:
        return objective_delta

    ratio_delta = float(left.get("stableRatio", 0)) - float(right.get("stableRatio", 0))
    if abs(ratio_delta) > 1e-9:
        return ratio_delta

    return float(left.get("score", 0)) - float(right.get("score", 0))


def sort_by_optimization_preference_desc(left: dict, right: dict) -> int:
    delta = optimization_preference(right, left)
    if delta > 0:
        return 1
    if delta < 0:
        return -1
    return 0


def has_meaningful_optimization_gain(winner: dict, current: dict, step_size: float) -> bool:
    base_gate = max(0.02, float(step_size or 0) * 0.08)
    unstable_relax = 0.008 if float(current.get("stableRatio", 0)) < 0.15 else 0
    gate = max(0.012, base_gate - unstable_relax)
    return float(winner.get("objective", 0)) > float(current.get("objective", 0)) + gate


def next_optimization_step(
    current_step: float,
    improved: bool,
    objective_delta: float,
    min_step: float,
    stable_ratio: float,
    plateau_streak: int,
) -> float:
    step = float(current_step or 0)
    floor = float(min_step or 0)
    streak = int(plateau_streak or 0)

    if improved:
        if float(objective_delta or 0) > 3.2:
            return min(0.72, max(floor, step * 1.05))

        shrink = 0.92 if float(objective_delta or 0) > 1.25 else 0.86
        return max(floor, step * shrink)

    if streak >= 4:
        reheated = step * (1.18 + min(0.36, (streak - 3) * 0.06))
        return min(0.68, max(floor, reheated))

    fallback_shrink = 0.66 if float(stable_ratio or 0) >= 0.34 else 0.76
    return max(floor, step * fallback_shrink)
