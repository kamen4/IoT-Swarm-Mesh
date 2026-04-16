"""Purpose: Trigger periodic DECAY epochs and apply attenuation schedule."""

from __future__ import annotations

from src.propagation.decay_model import apply_global_decay
from src.propagation.link_usage_tracker import decay_link_stats
from src.utils.math_utils import clamp, js_round


def maybe_run_decay_phase(state: dict) -> dict:
    interval = max(0, js_round(float(state["config"].get("decayIntervalSteps", 0))))
    if interval <= 0:
        return {
            "triggered": False,
            "epoch": int(state.get("decayEpoch", 0)),
            "percent": 0,
            "factor": 1,
        }

    if state["round"] <= 0 or state["round"] % interval != 0:
        return {
            "triggered": False,
            "epoch": int(state.get("decayEpoch", 0)),
            "percent": 0,
            "factor": 1,
        }

    percent = float(clamp(float(state["config"].get("decayPercent", 0)), 0, 0.8))
    factor = 1 - percent
    epoch = int(state.get("decayEpoch", 0)) + 1

    apply_global_decay(state["nodes"], state["estimates"], factor)
    decay_link_stats(state, factor)

    state["decayEpoch"] = epoch
    if not isinstance(state.get("decayHistory"), list):
        state["decayHistory"] = []

    state["decayHistory"].append(
        {
            "round": state["round"],
            "epoch": epoch,
            "percent": percent,
            "factor": factor,
        }
    )

    if len(state["decayHistory"]) > 200:
        state["decayHistory"] = state["decayHistory"][-200:]

    return {
        "triggered": True,
        "epoch": epoch,
        "percent": percent,
        "factor": factor,
    }
