"""Purpose: Run headless simulation up to max steps and collect snapshots."""

from __future__ import annotations

from src.config.config_normalizer import normalize_config
from src.export.snapshot_builder import build_final_snapshot, build_round_snapshot
from src.state.simulation_state import create_simulation_state
from src.state.simulation_step import advance_simulation_round


def run_no_ui_simulation(config: dict, max_steps: int | None) -> dict:
    normalized_config = normalize_config(config)
    state = create_simulation_state(normalized_config)
    snapshots: list[dict] = []

    raw_limit = max_steps or normalized_config.get("maxRounds") or 1
    limit = max(1, int(raw_limit))

    steps_executed = 0
    for _ in range(limit):
        result = advance_simulation_round(state)
        if not result["advanced"]:
            break
        steps_executed += 1
        snapshots.append(build_round_snapshot(state))

    final_snapshot = build_final_snapshot(state, snapshots, steps_executed)
    return {
        "state": state,
        "snapshots": snapshots,
        "stepsExecuted": steps_executed,
        "finalSnapshot": final_snapshot,
    }
