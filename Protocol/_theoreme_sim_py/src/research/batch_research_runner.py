"""Purpose: Run adaptive batch optimization across topologies with optional multiprocessing."""

from __future__ import annotations

import os
import sys
from concurrent.futures import ProcessPoolExecutor
from datetime import datetime, timezone
from functools import cmp_to_key

from src.config.config_normalizer import normalize_config
from src.core.random import create_seeded_rng
from src.research.network_matrix_parser import build_topology_matrix
from src.research.optimization.candidate_selection import (
    create_optimization_candidate,
    has_meaningful_optimization_gain,
    next_optimization_step,
    optimization_preference,
    sort_by_optimization_preference_desc,
)
from src.research.parameter_search_space import (
    OPTIMIZATION_PARAMETERS,
    clamp_vector,
    decode_optimization_vector,
    encode_optimization_vector,
    random_direction,
    summarize_optimization_parameters,
)
from src.research.workers.evaluation_worker import evaluate_seed_run
from src.utils.math_utils import clamp, js_round


def _clamp_int(value, min_value: int, max_value: int) -> int:
    try:
        numeric = float(value)
    except (TypeError, ValueError):
        return min_value
    return js_round(clamp(numeric, min_value, max_value))


def _average_score(items: list[dict]) -> float:
    if not items:
        return 0.0
    return sum(float(item.get("score", 0)) for item in items) / len(items)


def _standard_deviation(values: list[float]) -> float:
    if not values:
        return 0.0
    average = sum(float(value) for value in values) / len(values)
    variance = sum((float(value) - average) ** 2 for value in values) / len(values)
    return variance**0.5


def _move_vector(vector: list[float], direction: list[int], distance: float) -> list[float]:
    return clamp_vector(
        [float(value) + float(direction[index]) * float(distance) for index, value in enumerate(vector)]
    )


def _aggregate_verdict(avg_score: float, stable_ratio: float) -> str:
    if stable_ratio >= 0.66 or avg_score >= 82:
        return "STABLE"
    if avg_score >= 60:
        return "OSCILLATING"
    return "UNSTABLE"


def _auto_parallel_workers() -> int:
    cpu_count = os.cpu_count() or 2
    return max(1, min(8, cpu_count - 1))


def _can_use_process_pool() -> bool:
    main_module = sys.modules.get("__main__")
    main_file = getattr(main_module, "__file__", "")
    return bool(main_file) and not str(main_file).startswith("<")


def _evaluate_vector(
    *,
    vector: list[float],
    base_config: dict,
    topology: dict,
    seed_start: int,
    seed_count: int,
    rounds_per_check: int,
    stage_label: str,
    progress: dict,
    evaluation_runs: list[dict],
    on_progress,
    capture_artifacts: bool,
    parallel_workers: int,
) -> dict:
    config_template = decode_optimization_vector(vector, base_config)
    runs: list[dict] = []
    score_samples: list[float] = []
    tail_eligible_ratio_sum = 0.0
    flapping_avg_sum = 0.0

    payloads = [
        {
            "configTemplate": config_template,
            "topology": topology,
            "seed": seed_start + seed_offset,
            "roundsPerCheck": rounds_per_check,
            "captureArtifacts": capture_artifacts,
        }
        for seed_offset in range(seed_count)
    ]

    results: list[dict] = []

    use_parallel = parallel_workers > 1 and seed_count > 1
    if use_parallel:
        max_workers = min(parallel_workers, seed_count)
        with ProcessPoolExecutor(max_workers=max_workers) as pool:
            results = list(pool.map(evaluate_seed_run, payloads))

        for result in results:
            progress["completed"] += 1
            evaluation_runs.append(result["evaluationRun"])

            if callable(on_progress):
                on_progress(
                    {
                        "completed": progress["completed"],
                        "total": progress["total"],
                        "networkId": topology["id"],
                        "stageId": stage_label,
                        "candidateId": stage_label,
                        "seed": result["seed"],
                    }
                )
    else:
        for payload in payloads:
            result = evaluate_seed_run(payload)
            results.append(result)

            progress["completed"] += 1
            evaluation_runs.append(result["evaluationRun"])

            if callable(on_progress):
                on_progress(
                    {
                        "completed": progress["completed"],
                        "total": progress["total"],
                        "networkId": topology["id"],
                        "stageId": stage_label,
                        "candidateId": stage_label,
                        "seed": result["seed"],
                    }
                )

    results.sort(key=lambda item: int(item["seed"]))

    for result in results:
        runs.append(result["runRecord"])
        score_samples.append(float(result["score"]))
        tail_eligible_ratio_sum += float(result["tailEligibleRatio"])
        flapping_avg_sum += float(result["flappingAvg"])

    avg_score = _average_score(runs)
    stable_ratio = (
        len([item for item in runs if item.get("verdict") == "STABLE"]) / len(runs)
        if runs
        else 0
    )
    tail_eligible_ratio = tail_eligible_ratio_sum / max(1, len(runs))
    flapping_avg = flapping_avg_sum / max(1, len(runs))
    score_std_dev = _standard_deviation(score_samples)

    best_seed_run = max(runs, key=lambda item: float(item.get("score", 0))) if (capture_artifacts and runs) else None

    return {
        "avgScore": avg_score,
        "stableRatio": stable_ratio,
        "tailEligibleRatio": tail_eligible_ratio,
        "flappingAvg": flapping_avg,
        "scoreStdDev": score_std_dev,
        "verdict": _aggregate_verdict(avg_score, stable_ratio),
        "configTemplate": config_template,
        "bestSeedRun": best_seed_run,
    }


def run_batch_research_study(request: dict) -> dict:
    base_config = normalize_config(request.get("baseConfig") or {})

    seed_start = _clamp_int(request.get("seedStart", base_config["seed"]), 1, 9_999_999)
    seed_count = _clamp_int(request.get("seedCount", 3), 1, 20)
    rounds_per_check = _clamp_int(
        request.get("roundsPerCheck", base_config["maxRounds"]),
        20,
        20_000,
    )

    topology_matrix = build_topology_matrix(
        {
            "matrixText": request.get("matrixText", ""),
            "nodeCountMin": request.get("nodeCountMin", max(8, base_config["nodeCount"] - 12)),
            "nodeCountMax": request.get("nodeCountMax", base_config["nodeCount"] + 12),
            "nodeCountStep": request.get("nodeCountStep", 8),
            "linkRadiusMin": request.get("linkRadiusMin", max(60, base_config["linkRadius"] - 30)),
            "linkRadiusMax": request.get("linkRadiusMax", base_config["linkRadius"] + 30),
            "linkRadiusStep": request.get("linkRadiusStep", 20),
        }
    )

    optimization_iterations = _clamp_int(request.get("optimizationIterations", 12), 0, 40)
    optimization_mode = (
        "fixed-baseline" if optimization_iterations <= 0 else "adaptive-search"
    )

    requested_workers = _clamp_int(request.get("parallelWorkers", 0), 0, 128)
    parallel_workers = _auto_parallel_workers() if requested_workers <= 0 else requested_workers
    if parallel_workers > 1 and not _can_use_process_pool():
        parallel_workers = 1

    runs_per_topology = seed_count * (2 + optimization_iterations * 3)
    total_runs = len(topology_matrix) * runs_per_topology
    progress = {"completed": 0, "total": total_runs}

    on_progress = request.get("onProgress")
    if callable(on_progress):
        on_progress({"completed": 0, "total": total_runs})

    evaluation_runs: list[dict] = []
    networks: list[dict] = []

    for topology in topology_matrix:
        rng = create_seeded_rng(
            seed_start + topology["nodeCount"] * 4099 + topology["linkRadius"] * 131
        )

        vector = encode_optimization_vector(base_config)
        step_size = 0.44
        min_step = 0.02
        plateau_streak = 0
        optimization_trace: list[dict] = []

        base_evaluation = _evaluate_vector(
            vector=vector,
            base_config=base_config,
            topology=topology,
            seed_start=seed_start,
            seed_count=seed_count,
            rounds_per_check=rounds_per_check,
            stage_label="baseline",
            progress=progress,
            evaluation_runs=evaluation_runs,
            on_progress=on_progress,
            capture_artifacts=False,
            parallel_workers=parallel_workers,
        )

        current = create_optimization_candidate(
            {
                "mode": "baseline",
                "vector": vector,
                "score": base_evaluation["avgScore"],
                "stableRatio": base_evaluation["stableRatio"],
                "tailEligibleRatio": base_evaluation["tailEligibleRatio"],
                "flappingAvg": base_evaluation["flappingAvg"],
                "scoreStdDev": base_evaluation["scoreStdDev"],
            }
        )

        best = create_optimization_candidate(
            {
                "mode": "baseline",
                "vector": vector,
                "score": base_evaluation["avgScore"],
                "stableRatio": base_evaluation["stableRatio"],
                "tailEligibleRatio": base_evaluation["tailEligibleRatio"],
                "flappingAvg": base_evaluation["flappingAvg"],
                "scoreStdDev": base_evaluation["scoreStdDev"],
            }
        )

        optimization_trace.append(
            {
                "iteration": 0,
                "mode": "baseline",
                "stepSize": step_size,
                "currentScore": current["score"],
                "currentObjective": current["objective"],
                "currentTailEligibleRatio": current["tailEligibleRatio"],
                "currentFlappingAvg": current["flappingAvg"],
                "currentScoreStdDev": current["scoreStdDev"],
                "bestScore": best["score"],
                "bestObjective": best["objective"],
                "stableRatio": current["stableRatio"],
                "plateauStreak": plateau_streak,
            }
        )

        for iteration in range(1, optimization_iterations + 1):
            active_dimensions = 6 if best["score"] < 55 else 4 if best["score"] < 68 else 3
            direction = random_direction(rng, active_dimensions)

            plateau_boost = 1 + min(1.2, (plateau_streak - 2) * 0.22) if plateau_streak >= 3 else 1
            bad_state_boost = 1.55 if best["score"] < 55 else 1.2 if best["score"] < 68 else 1
            probe_distance = max(min_step, step_size * bad_state_boost * plateau_boost)

            plus_vector = _move_vector(vector, direction, probe_distance)
            minus_vector = _move_vector(vector, direction, -probe_distance)

            plus_evaluation = _evaluate_vector(
                vector=plus_vector,
                base_config=base_config,
                topology=topology,
                seed_start=seed_start,
                seed_count=seed_count,
                rounds_per_check=rounds_per_check,
                stage_label=f"iter-{iteration}-plus",
                progress=progress,
                evaluation_runs=evaluation_runs,
                on_progress=on_progress,
                capture_artifacts=False,
                parallel_workers=parallel_workers,
            )

            minus_evaluation = _evaluate_vector(
                vector=minus_vector,
                base_config=base_config,
                topology=topology,
                seed_start=seed_start,
                seed_count=seed_count,
                rounds_per_check=rounds_per_check,
                stage_label=f"iter-{iteration}-minus",
                progress=progress,
                evaluation_runs=evaluation_runs,
                on_progress=on_progress,
                capture_artifacts=False,
                parallel_workers=parallel_workers,
            )

            plus_candidate = create_optimization_candidate(
                {
                    "mode": "plus",
                    "vector": plus_vector,
                    "score": plus_evaluation["avgScore"],
                    "stableRatio": plus_evaluation["stableRatio"],
                    "tailEligibleRatio": plus_evaluation["tailEligibleRatio"],
                    "flappingAvg": plus_evaluation["flappingAvg"],
                    "scoreStdDev": plus_evaluation["scoreStdDev"],
                }
            )

            minus_candidate = create_optimization_candidate(
                {
                    "mode": "minus",
                    "vector": minus_vector,
                    "score": minus_evaluation["avgScore"],
                    "stableRatio": minus_evaluation["stableRatio"],
                    "tailEligibleRatio": minus_evaluation["tailEligibleRatio"],
                    "flappingAvg": minus_evaluation["flappingAvg"],
                    "scoreStdDev": minus_evaluation["scoreStdDev"],
                }
            )

            gradient_direction = 1 if optimization_preference(plus_candidate, minus_candidate) >= 0 else -1
            gradient_magnitude = abs(plus_candidate["objective"] - minus_candidate["objective"])

            move_strength = max(min_step, step_size * (0.65 + min(1.45, gradient_magnitude / 12)))
            gradient_vector = _move_vector(vector, direction, gradient_direction * move_strength)

            gradient_evaluation = _evaluate_vector(
                vector=gradient_vector,
                base_config=base_config,
                topology=topology,
                seed_start=seed_start,
                seed_count=seed_count,
                rounds_per_check=rounds_per_check,
                stage_label=f"iter-{iteration}-gradient",
                progress=progress,
                evaluation_runs=evaluation_runs,
                on_progress=on_progress,
                capture_artifacts=False,
                parallel_workers=parallel_workers,
            )

            gradient_candidate = create_optimization_candidate(
                {
                    "mode": "gradient",
                    "vector": gradient_vector,
                    "score": gradient_evaluation["avgScore"],
                    "stableRatio": gradient_evaluation["stableRatio"],
                    "tailEligibleRatio": gradient_evaluation["tailEligibleRatio"],
                    "flappingAvg": gradient_evaluation["flappingAvg"],
                    "scoreStdDev": gradient_evaluation["scoreStdDev"],
                }
            )

            options = [
                create_optimization_candidate(
                    {
                        "mode": "hold",
                        "vector": vector,
                        "score": current["score"],
                        "stableRatio": current["stableRatio"],
                        "tailEligibleRatio": current["tailEligibleRatio"],
                        "flappingAvg": current["flappingAvg"],
                        "scoreStdDev": current["scoreStdDev"],
                    }
                ),
                plus_candidate,
                minus_candidate,
                gradient_candidate,
            ]

            options.sort(key=cmp_to_key(sort_by_optimization_preference_desc))
            winner = options[0]
            selected = winner

            improved = has_meaningful_optimization_gain(winner, current, step_size)
            forced_explore = False

            if (not improved) and plateau_streak >= 4 and winner["mode"] == "hold":
                exploratory = next((option for option in options if option["mode"] != "hold"), None)
                if exploratory and optimization_preference(exploratory, current) > -0.8:
                    selected = dict(exploratory)
                    selected["mode"] = f"explore-{exploratory['mode']}"
                    forced_explore = True

            moved = improved or forced_explore
            objective_delta = float(selected["objective"]) - float(current["objective"])

            if moved:
                vector = selected["vector"]
                current = selected
                plateau_streak = 0
            else:
                plateau_streak += 1

            step_size = next_optimization_step(
                step_size,
                moved,
                objective_delta,
                min_step,
                current["stableRatio"],
                plateau_streak,
            )

            if optimization_preference(winner, best) > 0:
                best = winner

            optimization_trace.append(
                {
                    "iteration": iteration,
                    "mode": selected["mode"] if moved else winner["mode"],
                    "stepSize": step_size,
                    "currentScore": current["score"],
                    "currentObjective": current["objective"],
                    "currentTailEligibleRatio": current["tailEligibleRatio"],
                    "currentFlappingAvg": current["flappingAvg"],
                    "currentScoreStdDev": current["scoreStdDev"],
                    "bestScore": best["score"],
                    "bestObjective": best["objective"],
                    "stableRatio": current["stableRatio"],
                    "plateauStreak": plateau_streak,
                    "activeDimensions": active_dimensions,
                }
            )

        best_detailed = _evaluate_vector(
            vector=best["vector"],
            base_config=base_config,
            topology=topology,
            seed_start=seed_start,
            seed_count=seed_count,
            rounds_per_check=rounds_per_check,
            stage_label="best-final",
            progress=progress,
            evaluation_runs=evaluation_runs,
            on_progress=on_progress,
            capture_artifacts=True,
            parallel_workers=parallel_workers,
        )

        best_seed_run = best_detailed.get("bestSeedRun")
        best_config = (
            best_seed_run.get("config") if best_seed_run is not None else best_detailed["configTemplate"]
        )

        networks.append(
            {
                **topology,
                "optimizationTrace": optimization_trace,
                "best": {
                    "avgScore": best_detailed["avgScore"],
                    "stableRatio": best_detailed["stableRatio"],
                    "verdict": _aggregate_verdict(
                        best_detailed["avgScore"],
                        best_detailed["stableRatio"],
                    ),
                    "objective": best["objective"],
                    "seed": best_seed_run.get("seed") if best_seed_run else seed_start,
                    "config": best_config,
                    "parameters": summarize_optimization_parameters(best_config),
                    "snapshots": best_seed_run.get("snapshots", []) if best_seed_run else [],
                    "scoreMetrics": best_seed_run.get("scoreMetrics") if best_seed_run else None,
                    "scoreRationale": best_seed_run.get("scoreRationale", []) if best_seed_run else [],
                    "topology": best_seed_run.get("topology") if best_seed_run else None,
                },
            }
        )

    recommendations = [
        {
            "networkId": network["id"],
            "label": network["label"],
            "nodeCount": network["nodeCount"],
            "linkRadius": network["linkRadius"],
            "optimizer": (
                "Fixed baseline (no optimization)"
                if optimization_iterations <= 0
                else "Adaptive gradient search + plateau escape"
            ),
            "avgScore": network["best"]["avgScore"],
            "stableRatio": network["best"]["stableRatio"],
            "bestSeed": network["best"]["seed"],
            "verdict": network["best"]["verdict"],
            "bestParameters": network["best"]["parameters"],
        }
        for network in networks
    ]

    generated_at = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")

    return {
        "metadata": {
            "generatedAt": generated_at,
            "seedStart": seed_start,
            "seedCount": seed_count,
            "roundsPerCheck": rounds_per_check,
            "totalRuns": total_runs,
            "topologyCount": len(topology_matrix),
            "optimizationIterations": optimization_iterations,
            "optimizationMode": optimization_mode,
            "parallelWorkers": parallel_workers,
        },
        "tunedParameters": [
            {
                "key": item["key"],
                "label": item["label"],
                "type": item["type"],
                "min": item["min"],
                "max": item["max"],
            }
            for item in OPTIMIZATION_PARAMETERS
        ],
        "evaluationRuns": evaluation_runs,
        "networks": networks,
        "recommendations": recommendations,
    }
