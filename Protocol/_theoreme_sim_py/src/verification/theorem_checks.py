"""Purpose: Evaluate theorem lemmas and aggregated theorem status over eligible parent graph."""

from __future__ import annotations

from src.core.constants import THEOREM_ROOT_ID
from src.verification.assumption_checks import check_assumptions, get_eligible_node_ids


def _check_lemma41_strict_increase(state: dict) -> dict:
    violations: list[int] = []

    for node in state["nodes"].values():
        if (not node.eligible) or node.is_gateway or node.parent is None:
            continue
        parent = state["nodes"].get(node.parent)
        if parent is None or not (float(parent.q_total) > float(node.q_total)):
            violations.append(node.id)

    return {"pass": len(violations) == 0, "violations": violations}


def _check_lemma42_acyclic(state: dict) -> dict:
    eligible_set = set(get_eligible_node_ids(state))
    visited_global: set[int] = set()

    for start_id in sorted(eligible_set):
        if start_id in visited_global:
            continue

        local_index: dict[int, int] = {}
        path: list[int] = []
        current: int | None = start_id

        while current is not None and current in eligible_set:
            if current in local_index:
                cycle_start = local_index[current]
                return {"pass": False, "cycleWitness": path[cycle_start:]}
            if current in visited_global:
                break

            local_index[current] = len(path)
            path.append(current)
            visited_global.add(current)
            current = state["parentMap"].get(current)

    return {"pass": True, "cycleWitness": []}


def _check_lemma43_reachability(state: dict) -> dict:
    eligible = get_eligible_node_ids(state)
    unreachable: list[int] = []

    for node_id in eligible:
        if node_id == THEOREM_ROOT_ID:
            continue

        seen: set[int] = set()
        current: int | None = node_id
        reached = False

        while current is not None:
            if current in seen:
                reached = False
                break
            seen.add(current)

            if current == THEOREM_ROOT_ID:
                reached = True
                break

            current = state["parentMap"].get(current)

        if not reached:
            unreachable.append(node_id)

    return {"pass": len(unreachable) == 0, "unreachable": unreachable}


def evaluate_theorem(state: dict) -> dict:
    eligible_count = len(get_eligible_node_ids(state))
    eligible_non_root = max(0, eligible_count - 1)

    if eligible_non_root == 0:
        return {
            "assumptionsPass": None,
            "theoremPass": None,
            "eligibleCount": eligible_count,
            "a5": None,
            "a6": None,
            "a7": None,
            "lemma41": None,
            "lemma42": None,
            "lemma43": None,
            "violationsA6": [],
            "unreachable": [],
            "cycleWitness": [],
            "verificationState": "pending",
        }

    assumptions = check_assumptions(state)
    lemma41 = _check_lemma41_strict_increase(state)
    lemma42 = _check_lemma42_acyclic(state)
    lemma43 = _check_lemma43_reachability(state)

    assigned_parents = 0
    for child_id, parent_id in state["parentMap"].items():
        if (
            child_id != THEOREM_ROOT_ID
            and parent_id is not None
            and state["nodes"][child_id].eligible
        ):
            assigned_parents += 1

    spanning_condition = assigned_parents >= eligible_non_root

    assumptions_pass = (
        bool(assumptions["a5"]["pass"])
        and bool(assumptions["a6"]["pass"])
        and bool(assumptions["a7"]["pass"])
    )

    theorem_pass = (
        assumptions_pass
        and bool(lemma41["pass"])
        and bool(lemma42["pass"])
        and bool(lemma43["pass"])
        and spanning_condition
    )

    return {
        "assumptionsPass": assumptions_pass,
        "theoremPass": theorem_pass,
        "eligibleCount": eligible_count,
        "a5": assumptions["a5"]["pass"],
        "a6": assumptions["a6"]["pass"],
        "a7": assumptions["a7"]["pass"],
        "lemma41": lemma41["pass"],
        "lemma42": lemma42["pass"],
        "lemma43": lemma43["pass"],
        "violationsA6": assumptions["a6"]["violations"],
        "unreachable": lemma43["unreachable"],
        "cycleWitness": lemma42["cycleWitness"],
        "verificationState": "pass" if theorem_pass else "fail",
    }
