"""Purpose: Execute one full simulation round in headless mode."""

from __future__ import annotations

from src.phases.decay_phase import maybe_run_decay_phase
from src.phases.down_round_phase import run_down_round_phase
from src.phases.up_round_phase import run_up_round_phase
from src.propagation.charge_spread_round import apply_charge_spread_round
from src.propagation.link_usage_tracker import finalize_link_strength_round
from src.propagation.neighbor_charge_propagation import propagate_neighbor_charges_round
from src.routing.tree_builder import rebuild_tree
from src.state.simulation_state import refresh_charge_bounds, refresh_eligibility
from src.verification.oscillation_detector import update_oscillation_report
from src.verification.theorem_checks import evaluate_theorem


def advance_simulation_round(state: dict) -> dict:
    if state is None:
        return {"advanced": False}

    if int(state["round"]) >= int(state["config"]["maxRounds"]):
        return {"advanced": False}

    state["round"] += 1

    state["lastBroadcastReport"] = run_down_round_phase(state)
    state["lastUp"] = run_up_round_phase(state)
    state["lastPropagation"] = propagate_neighbor_charges_round(state)
    state["lastSpread"] = apply_charge_spread_round(state)

    finalize_link_strength_round(state)

    refresh_eligibility(state["nodes"], state["config"]["qForward"])

    rebuilt = rebuild_tree(state)
    state["parentMap"] = rebuilt["parentMap"]
    state["childrenMap"] = rebuilt["childrenMap"]
    final_changed_count = rebuilt["changedCount"]

    state["lastDecay"] = maybe_run_decay_phase(state)
    if state["lastDecay"]["triggered"]:
        refresh_eligibility(state["nodes"], state["config"]["qForward"])
        rebuilt = rebuild_tree(state)
        state["parentMap"] = rebuilt["parentMap"]
        state["childrenMap"] = rebuilt["childrenMap"]
        final_changed_count = rebuilt["changedCount"]

    state["stableRounds"] = (
        int(state["stableRounds"]) + 1 if final_changed_count == 0 else 0
    )

    refresh_charge_bounds(state)
    state["lastOscillationReport"] = update_oscillation_report(state)
    state["lastTheoremReport"] = evaluate_theorem(state)

    if not isinstance(state.get("broadcastHistory"), list):
        state["broadcastHistory"] = []

    state["broadcastHistory"].append(
        {
            "round": state["round"],
            "duplicates": state["lastBroadcastReport"]["duplicates"],
            "reachedCount": state["lastBroadcastReport"]["reachedCount"],
            "coverage": state["lastBroadcastReport"]["coverage"],
        }
    )

    if len(state["broadcastHistory"]) > 300:
        state["broadcastHistory"] = state["broadcastHistory"][-300:]

    return {"advanced": True}
