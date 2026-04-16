"""Purpose: Build compact pass/fail counters from round snapshots."""

from __future__ import annotations


def _count_pass(snapshots: list[dict], key: str) -> int:
    return sum(1 for item in snapshots if item.get(key) is True)


def _ratio(count: int, total: int) -> float:
    if total <= 0:
        return 0.0
    return count / total


def _average_value(snapshots: list[dict], key: str) -> float:
    values = [float(item.get(key, 0)) for item in snapshots]
    if not values:
        return 0.0
    return sum(values) / len(values)


def build_compact_pass_metrics(snapshots: list[dict] | None) -> dict:
    source = snapshots or []
    rounds = len(source)

    theorem_pass_count = _count_pass(source, "theoremPass")
    assumptions_pass_count = _count_pass(source, "assumptionsPass")
    a5_pass_count = _count_pass(source, "a5")
    a6_pass_count = _count_pass(source, "a6")
    a7_pass_count = _count_pass(source, "a7")
    lemma41_pass_count = _count_pass(source, "lemma41")
    lemma42_pass_count = _count_pass(source, "lemma42")
    lemma43_pass_count = _count_pass(source, "lemma43")

    pending_rounds = sum(1 for item in source if item.get("verificationState") == "pending")

    return {
        "rounds": rounds,
        "pendingRounds": pending_rounds,
        "theoremPassCount": theorem_pass_count,
        "theoremPassRate": _ratio(theorem_pass_count, rounds),
        "assumptionsPassCount": assumptions_pass_count,
        "assumptionsPassRate": _ratio(assumptions_pass_count, rounds),
        "a5PassCount": a5_pass_count,
        "a5PassRate": _ratio(a5_pass_count, rounds),
        "a6PassCount": a6_pass_count,
        "a6PassRate": _ratio(a6_pass_count, rounds),
        "a7PassCount": a7_pass_count,
        "a7PassRate": _ratio(a7_pass_count, rounds),
        "lemma41PassCount": lemma41_pass_count,
        "lemma41PassRate": _ratio(lemma41_pass_count, rounds),
        "lemma42PassCount": lemma42_pass_count,
        "lemma42PassRate": _ratio(lemma42_pass_count, rounds),
        "lemma43PassCount": lemma43_pass_count,
        "lemma43PassRate": _ratio(lemma43_pass_count, rounds),
        "avgEligibleCount": _average_value(source, "eligibleCount"),
        "avgParentChanges": _average_value(source, "parentChanges"),
        "avgFlappingNodes": _average_value(source, "flappingNodes"),
    }
