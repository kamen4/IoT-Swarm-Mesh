"""Purpose: Convert simulation snapshots into comparable stability score and verdict."""

from __future__ import annotations


def _average(values: list[float]) -> float:
    if not values:
        return 0.0
    return sum(values) / len(values)


def _trend_delta(values: list[float]) -> dict:
    if not values:
        return {"early": 0.0, "late": 0.0, "delta": 0.0}

    chunk = max(1, int(len(values) * 0.25))
    early = _average(values[:chunk])
    late = _average(values[-chunk:])
    return {"early": early, "late": late, "delta": early - late}


def _tail_eligible_health(snapshots: list[dict]) -> dict:
    if not snapshots:
        return {"tailRatio": 0.0, "maxTail": 0.0, "minTail": 0.0}

    chunk = max(3, int(len(snapshots) * 0.2))
    tail = [float(item.get("eligibleCount", 0)) for item in snapshots[-chunk:]]
    max_tail = max([0.0, *tail])
    min_tail = min(tail) if tail else 0.0

    return {
        "tailRatio": (min_tail / max_tail) if max_tail > 0 else 0.0,
        "maxTail": max_tail,
        "minTail": min_tail,
    }


def score_stability(run: dict) -> dict:
    snapshots = run.get("snapshots", [])
    total_rounds = len(snapshots)

    if total_rounds == 0:
        return {
            "score": 0,
            "verdict": "UNSTABLE",
            "metrics": {"totalRounds": 0},
            "rationale": ["No rounds executed."],
        }

    theorem_pass_rate = (
        len([item for item in snapshots if item.get("theoremPass") is True])
        / total_rounds
    )
    assumptions_pass_rate = (
        len([item for item in snapshots if item.get("assumptionsPass") is True])
        / total_rounds
    )

    coverage = [float(item.get("downCoverage", 0)) for item in snapshots]
    coverage_avg = _average(coverage)

    duplicates = [float(item.get("downDuplicates", 0)) for item in snapshots]
    duplicate_trend = _trend_delta(duplicates)

    parent_changes = [float(item.get("parentChanges", 0)) for item in snapshots]
    parent_change_avg = _average(parent_changes)

    flapping = [float(item.get("flappingNodes", 0)) for item in snapshots]
    flapping_avg = _average(flapping)

    eligible_health = _tail_eligible_health(snapshots)

    theorem_score = theorem_pass_rate * 30
    assumptions_score = assumptions_pass_rate * 18
    coverage_score = min(1.0, coverage_avg / 0.95) * 18
    duplicate_score = (
        min(1.0, duplicate_trend["delta"] / max(1.0, duplicate_trend["early"])) * 16
        if duplicate_trend["delta"] > 0
        else 0
    )

    parent_score = (
        max(0.0, 10 - min(10, parent_change_avg))
        + max(0.0, 8 - min(8, flapping_avg * 1.5))
    )

    score = theorem_score + assumptions_score + coverage_score + duplicate_score + parent_score
    rationale: list[str] = []

    if eligible_health["tailRatio"] < 0.6:
        score *= 0.7
        rationale.append("Eligible-set collapse detected in tail rounds.")

    tail8 = snapshots[-8:]
    if tail8 and all(float(item.get("eligibleCount", 0)) <= 1 for item in tail8):
        score *= 0.45
        rationale.append("Vacuous tail: only gateway remains eligible in final rounds.")

    if flapping_avg > 2.2:
        score *= 0.82
        rationale.append("High average flapping detected.")

    score = max(0.0, min(100.0, score))

    if score >= 80:
        verdict = "STABLE"
    elif score >= 60:
        verdict = "OSCILLATING"
    else:
        verdict = "UNSTABLE"

    return {
        "score": score,
        "verdict": verdict,
        "metrics": {
            "totalRounds": total_rounds,
            "theoremPassRate": theorem_pass_rate,
            "assumptionsPassRate": assumptions_pass_rate,
            "coverageAvg": coverage_avg,
            "duplicateEarly": duplicate_trend["early"],
            "duplicateLate": duplicate_trend["late"],
            "duplicateDrop": duplicate_trend["delta"],
            "parentChangeAvg": parent_change_avg,
            "flappingAvg": flapping_avg,
            "eligibleTailRatio": eligible_health["tailRatio"],
            "eligibleTailMin": eligible_health["minTail"],
            "eligibleTailMax": eligible_health["maxTail"],
        },
        "rationale": rationale,
    }
