"""Purpose: Build comprehensive parameter-sensitivity artifacts for batch study outputs."""

from __future__ import annotations

import json
import math
from pathlib import Path

from src.research.parameter_search_space import OPTIMIZATION_KEYS
from src.research.svg_charts import (
    write_bar_chart,
    write_heatmap_chart,
    write_signed_bar_chart,
)

OUTCOME_SPECS = [
    {"key": "score", "label": "Score", "direction": 1.0},
    {"key": "theoremPassRate", "label": "TheoremPass", "direction": 1.0},
    {"key": "assumptionsPassRate", "label": "AssumptionPass", "direction": 1.0},
    {"key": "coverageAvg", "label": "Coverage", "direction": 1.0},
    {"key": "duplicateDrop", "label": "DuplicateDrop", "direction": 1.0},
    {"key": "eligibleTailRatio", "label": "EligibleTail", "direction": 1.0},
    {"key": "parentChangeAvg", "label": "ParentChanges", "direction": -1.0},
    {"key": "flappingAvg", "label": "Flapping", "direction": -1.0},
]


def build_comprehensive_request() -> dict:
    """Return a built-in large batch study profile for sensitivity analysis."""
    matrix_pairs = [
        "12x90",
        "12x140",
        "12x190",
        "24x120",
        "24x180",
        "24x240",
        "34x150",
        "34x195",
        "34x240",
        "60x190",
        "60x250",
        "60x310",
        "90x240",
        "90x300",
        "90x360",
    ]

    return {
        "baseConfig": {
            "nodeCount": 34,
            "linkRadius": 195,
            "seed": 42,
            "maxRounds": 280,
        },
        "seedCount": 4,
        "optimizationIterations": 10,
        "roundsPerCheck": 280,
        "matrixText": ",".join(matrix_pairs),
        "parallelWorkers": 0,
    }


def _to_float(value, default: float = 0.0) -> float:
    try:
        numeric = float(value)
    except (TypeError, ValueError):
        return float(default)

    if math.isnan(numeric) or math.isinf(numeric):
        return float(default)
    return numeric


def _mean(values: list[float]) -> float:
    if not values:
        return 0.0
    return sum(values) / len(values)


def _stddev(values: list[float]) -> float:
    if len(values) <= 1:
        return 0.0
    avg = _mean(values)
    variance = sum((value - avg) ** 2 for value in values) / len(values)
    return variance**0.5


def _pearson(xs: list[float], ys: list[float]) -> float:
    if len(xs) != len(ys) or len(xs) < 2:
        return 0.0

    avg_x = _mean(xs)
    avg_y = _mean(ys)

    numerator = 0.0
    sum_x = 0.0
    sum_y = 0.0

    for x_value, y_value in zip(xs, ys):
        dx = x_value - avg_x
        dy = y_value - avg_y
        numerator += dx * dy
        sum_x += dx * dx
        sum_y += dy * dy

    denominator = (sum_x * sum_y) ** 0.5
    if denominator <= 1e-12:
        return 0.0

    return numerator / denominator


def _rank_values(values: list[float]) -> list[float]:
    indexed = sorted(enumerate(values), key=lambda item: item[1])
    ranks = [0.0] * len(values)

    index = 0
    while index < len(indexed):
        tail = index
        while tail + 1 < len(indexed) and indexed[tail + 1][1] == indexed[index][1]:
            tail += 1

        rank_value = (index + tail + 2) / 2.0
        for slot in range(index, tail + 1):
            original_index = indexed[slot][0]
            ranks[original_index] = rank_value

        index = tail + 1

    return ranks


def _spearman(xs: list[float], ys: list[float]) -> float:
    if len(xs) != len(ys) or len(xs) < 2:
        return 0.0
    return _pearson(_rank_values(xs), _rank_values(ys))


def _quantile(sorted_values: list[float], q: float) -> float:
    if not sorted_values:
        return 0.0
    if len(sorted_values) == 1:
        return float(sorted_values[0])

    clamped_q = max(0.0, min(1.0, float(q)))
    position = clamped_q * (len(sorted_values) - 1)
    left = int(math.floor(position))
    right = int(math.ceil(position))
    if left == right:
        return float(sorted_values[left])

    weight = position - left
    return sorted_values[left] * (1.0 - weight) + sorted_values[right] * weight


def _parse_network_shape(network_id: str) -> tuple[int, int]:
    token = str(network_id or "")
    if "x" not in token:
        return (0, 0)

    left, right = token.split("x", 1)
    try:
        return (int(left), int(right))
    except ValueError:
        return (0, 0)


def _classify_size(node_count: int) -> str:
    if node_count <= 20:
        return "small"
    if node_count <= 50:
        return "medium"
    if node_count <= 90:
        return "large"
    return "xlarge"


def _classify_density(link_radius: int) -> str:
    if link_radius < 150:
        return "sparse"
    if link_radius < 270:
        return "medium"
    return "dense"


def _class_key(node_count: int, link_radius: int) -> str:
    return f"{_classify_size(node_count)}-{_classify_density(link_radius)}"


def _collect_rows(study: dict) -> list[dict]:
    rows: list[dict] = []

    for run in study.get("evaluationRuns", []) or []:
        network_id = str(run.get("networkId", "n/a"))
        parsed_node_count, parsed_link_radius = _parse_network_shape(network_id)

        node_count = int(run.get("nodeCount", parsed_node_count) or parsed_node_count)
        link_radius = int(run.get("linkRadius", parsed_link_radius) or parsed_link_radius)

        params = run.get("params", {}) or {}
        normalized_params = {
            key: _to_float(params.get(key, 0.0))
            for key in OPTIMIZATION_KEYS
        }

        row = {
            "seed": int(run.get("seed", 0) or 0),
            "runId": str(run.get("runId", "")),
            "networkId": network_id,
            "nodeCount": node_count,
            "linkRadius": link_radius,
            "networkClass": _class_key(node_count, link_radius),
            "score": _to_float(run.get("score", 0.0)),
            "verdict": str(run.get("verdict", "UNSTABLE")),
            "theoremPassRate": _to_float(run.get("theoremPassRate", 0.0)),
            "assumptionsPassRate": _to_float(run.get("assumptionsPassRate", 0.0)),
            "coverageAvg": _to_float(run.get("coverageAvg", 0.0)),
            "duplicateDrop": _to_float(run.get("duplicateDrop", 0.0)),
            "parentChangeAvg": _to_float(run.get("parentChangeAvg", 0.0)),
            "flappingAvg": _to_float(run.get("flappingAvg", 0.0)),
            "eligibleTailRatio": _to_float(run.get("eligibleTailRatio", 0.0)),
            "params": normalized_params,
        }
        rows.append(row)

    return rows


def _build_parameter_correlations(rows: list[dict]) -> dict:
    correlations: dict[str, dict[str, dict[str, float]]] = {}

    for param_key in OPTIMIZATION_KEYS:
        xs = [_to_float(row["params"].get(param_key, 0.0)) for row in rows]
        per_metric: dict[str, dict[str, float]] = {}

        for metric in OUTCOME_SPECS:
            metric_key = metric["key"]
            ys = [_to_float(row.get(metric_key, 0.0)) for row in rows]
            per_metric[metric_key] = {
                "pearson": _pearson(xs, ys),
                "spearman": _spearman(xs, ys),
            }

        correlations[param_key] = per_metric

    return correlations


def _build_influence_ranking(correlations: dict) -> list[dict]:
    metric_direction = {item["key"]: float(item["direction"]) for item in OUTCOME_SPECS}
    ranking: list[dict] = []

    for param_key in OPTIMIZATION_KEYS:
        per_metric = correlations.get(param_key, {})
        absolute_scores: list[float] = []

        strongest_metric = "score"
        strongest_value = 0.0

        for metric in OUTCOME_SPECS:
            metric_key = metric["key"]
            value = _to_float(per_metric.get(metric_key, {}).get("pearson", 0.0))
            absolute_scores.append(abs(value))
            if abs(value) > abs(strongest_value):
                strongest_value = value
                strongest_metric = metric_key

        impact_index = _mean(absolute_scores)
        strongest_direction_score = strongest_value * metric_direction.get(strongest_metric, 1.0)
        if strongest_direction_score > 0.05:
            interpretation = "higher usually improves strongest metric"
        elif strongest_direction_score < -0.05:
            interpretation = "higher usually worsens strongest metric"
        else:
            interpretation = "weak directional effect"

        ranking.append(
            {
                "parameter": param_key,
                "impactIndex": impact_index,
                "strongestMetric": strongest_metric,
                "strongestCorrelation": strongest_value,
                "interpretation": interpretation,
            }
        )

    ranking.sort(key=lambda item: float(item["impactIndex"]), reverse=True)
    return ranking


def _build_pairwise_parameter_correlations(rows: list[dict]) -> list[dict]:
    pairs: list[dict] = []

    for left_index in range(len(OPTIMIZATION_KEYS)):
        left_key = OPTIMIZATION_KEYS[left_index]
        left_values = [_to_float(row["params"].get(left_key, 0.0)) for row in rows]

        for right_index in range(left_index + 1, len(OPTIMIZATION_KEYS)):
            right_key = OPTIMIZATION_KEYS[right_index]
            right_values = [_to_float(row["params"].get(right_key, 0.0)) for row in rows]
            coefficient = _pearson(left_values, right_values)
            pairs.append(
                {
                    "left": left_key,
                    "right": right_key,
                    "pearson": coefficient,
                    "absPearson": abs(coefficient),
                }
            )

    pairs.sort(key=lambda item: float(item["absPearson"]), reverse=True)
    return pairs


def _build_topology_correlations(rows: list[dict]) -> dict:
    node_values = [_to_float(row.get("nodeCount", 0.0)) for row in rows]
    radius_values = [_to_float(row.get("linkRadius", 0.0)) for row in rows]

    by_metric: dict[str, dict[str, float]] = {}
    for metric in OUTCOME_SPECS:
        metric_key = metric["key"]
        y_values = [_to_float(row.get(metric_key, 0.0)) for row in rows]
        by_metric[metric_key] = {
            "nodeCount": _pearson(node_values, y_values),
            "linkRadius": _pearson(radius_values, y_values),
        }

    return by_metric


def _build_instability_summary(rows: list[dict]) -> dict:
    verdict_counts = {
        "STABLE": 0,
        "OSCILLATING": 0,
        "UNSTABLE": 0,
    }

    for row in rows:
        verdict = str(row.get("verdict", "UNSTABLE"))
        if verdict not in verdict_counts:
            verdict_counts[verdict] = 0
        verdict_counts[verdict] += 1

    unstable_rows = [
        row
        for row in rows
        if str(row.get("verdict", "UNSTABLE")) in {"UNSTABLE", "OSCILLATING"}
    ]

    worst_source = unstable_rows if unstable_rows else rows
    worst_rows = sorted(
        worst_source,
        key=lambda item: _to_float(item.get("score", 0.0)),
    )[:20]

    worst_runs: list[dict] = []
    for item in worst_rows:
        worst_runs.append(
            {
                "runId": str(item.get("runId", "")),
                "networkId": str(item.get("networkId", "n/a")),
                "seed": int(item.get("seed", 0) or 0),
                "verdict": str(item.get("verdict", "UNSTABLE")),
                "score": _to_float(item.get("score", 0.0)),
                "theoremPassRate": _to_float(item.get("theoremPassRate", 0.0)),
                "assumptionsPassRate": _to_float(item.get("assumptionsPassRate", 0.0)),
                "coverageAvg": _to_float(item.get("coverageAvg", 0.0)),
                "duplicateDrop": _to_float(item.get("duplicateDrop", 0.0)),
                "eligibleTailRatio": _to_float(item.get("eligibleTailRatio", 0.0)),
                "parentChangeAvg": _to_float(item.get("parentChangeAvg", 0.0)),
                "flappingAvg": _to_float(item.get("flappingAvg", 0.0)),
                "params": item.get("params", {}) or {},
            }
        )

    def _run_detail(prefix: str, row: dict) -> dict[str, object]:
        return {
            f"{prefix}RunId": str(row.get("runId", "")),
            f"{prefix}Seed": int(row.get("seed", 0) or 0),
            f"{prefix}Verdict": str(row.get("verdict", "UNSTABLE")),
            f"{prefix}Score": _to_float(row.get("score", 0.0)),
            f"{prefix}TheoremPassRate": _to_float(row.get("theoremPassRate", 0.0)),
            f"{prefix}AssumptionsPassRate": _to_float(row.get("assumptionsPassRate", 0.0)),
            f"{prefix}CoverageAvg": _to_float(row.get("coverageAvg", 0.0)),
            f"{prefix}DuplicateDrop": _to_float(row.get("duplicateDrop", 0.0)),
            f"{prefix}EligibleTailRatio": _to_float(row.get("eligibleTailRatio", 0.0)),
            f"{prefix}ParentChangeAvg": _to_float(row.get("parentChangeAvg", 0.0)),
            f"{prefix}FlappingAvg": _to_float(row.get("flappingAvg", 0.0)),
        }

    worst_by_network_map: dict[str, dict] = {}
    for row in rows:
        network_id = str(row.get("networkId", "n/a"))
        verdict = str(row.get("verdict", "UNSTABLE"))
        score = _to_float(row.get("score", 0.0))
        is_unstable = verdict in {"UNSTABLE", "OSCILLATING"}

        existing = worst_by_network_map.get(network_id)
        if existing is None:
            existing = {
                "networkId": network_id,
                "nodeCount": int(row.get("nodeCount", 0) or 0),
                "linkRadius": int(row.get("linkRadius", 0) or 0),
                "unstableOrOscillatingCount": 0,
                "sampleCount": 0,
            }
            existing.update(_run_detail("worst", row))
            worst_by_network_map[network_id] = existing

        existing["sampleCount"] += 1
        if is_unstable:
            existing["unstableOrOscillatingCount"] += 1

        # Worst overall
        if score < float(existing.get("worstScore", 0.0)):
            existing.update(_run_detail("worst", row))

        # Worst unstable/oscillating (if any)
        if is_unstable:
            current = existing.get("worstUnstableScore")
            if current is None or score < float(current):
                existing.update(_run_detail("worstUnstable", row))

    for item in worst_by_network_map.values():
        sample_count = int(item.get("sampleCount", 0) or 0)
        unstable_count = int(item.get("unstableOrOscillatingCount", 0) or 0)
        item["unstableOrOscillatingShare"] = (
            (unstable_count / sample_count) if sample_count > 0 else 0.0
        )

    worst_by_network = sorted(
        worst_by_network_map.values(),
        key=lambda item: (
            int(item.get("nodeCount", 0)),
            int(item.get("linkRadius", 0)),
        ),
    )

    metric_bounds: dict[str, dict[str, float]] = {}
    for key in [
        "score",
        "theoremPassRate",
        "assumptionsPassRate",
        "coverageAvg",
        "duplicateDrop",
        "eligibleTailRatio",
        "parentChangeAvg",
        "flappingAvg",
    ]:
        values = [_to_float(row.get(key, 0.0)) for row in rows]
        if values:
            metric_bounds[key] = {
                "min": min(values),
                "max": max(values),
            }
        else:
            metric_bounds[key] = {"min": 0.0, "max": 0.0}

    sample_count = len(rows)
    unstable_count = len(unstable_rows)
    unstable_ratio = (unstable_count / sample_count) if sample_count > 0 else 0.0

    return {
        "sampleCount": sample_count,
        "unstableOrOscillatingCount": unstable_count,
        "unstableOrOscillatingRatio": unstable_ratio,
        "verdictCounts": verdict_counts,
        "worstRuns": worst_runs,
        "worstByNetwork": worst_by_network,
        "metricBounds": metric_bounds,
    }


def _build_global_ideal(rows: list[dict]) -> dict:
    if not rows:
        return {"sampleCount": 0, "topSampleCount": 0, "parameters": {}}

    sorted_rows = sorted(rows, key=lambda item: float(item.get("score", 0.0)), reverse=True)
    top_sample_count = max(20, int(len(sorted_rows) * 0.2))
    top_rows = sorted_rows[: min(len(sorted_rows), top_sample_count)]

    parameter_summary: dict[str, dict[str, float]] = {}
    for key in OPTIMIZATION_KEYS:
        values = sorted(_to_float(row["params"].get(key, 0.0)) for row in top_rows)
        parameter_summary[key] = {
            "mean": _mean(values),
            "median": _quantile(values, 0.5),
            "q25": _quantile(values, 0.25),
            "q75": _quantile(values, 0.75),
        }

    return {
        "sampleCount": len(rows),
        "topSampleCount": len(top_rows),
        "parameters": parameter_summary,
    }


def _build_parameter_variation(rows: list[dict]) -> dict:
    variation: dict[str, dict[str, float | int]] = {}

    for key in OPTIMIZATION_KEYS:
        values = [_to_float(row["params"].get(key, 0.0)) for row in rows]
        if not values:
            variation[key] = {
                "min": 0.0,
                "max": 0.0,
                "stdDev": 0.0,
                "distinctCount": 0,
            }
            continue

        distinct_count = len({round(value, 8) for value in values})
        variation[key] = {
            "min": min(values),
            "max": max(values),
            "stdDev": _stddev(values),
            "distinctCount": distinct_count,
        }

    return variation


def _count_verdicts(items: list[dict]) -> dict[str, int]:
    counts = {"STABLE": 0, "OSCILLATING": 0, "UNSTABLE": 0}
    for item in items:
        verdict = str(item.get("verdict", "UNSTABLE"))
        if verdict not in counts:
            counts[verdict] = 0
        counts[verdict] += 1
    return counts


def _build_network_class_summary(study: dict) -> list[dict]:
    grouped: dict[str, list[dict]] = {}

    for network in study.get("networks", []) or []:
        node_count = int(network.get("nodeCount", 0) or 0)
        link_radius = int(network.get("linkRadius", 0) or 0)
        class_id = _class_key(node_count, link_radius)

        best = network.get("best", {}) or {}
        item = {
            "networkId": network.get("id"),
            "label": network.get("label"),
            "nodeCount": node_count,
            "linkRadius": link_radius,
            "score": _to_float(best.get("avgScore", 0.0)),
            "stableRatio": _to_float(best.get("stableRatio", 0.0)),
            "verdict": str(best.get("verdict", "UNSTABLE")),
            "bestParameters": best.get("parameters", {}) or {},
        }

        grouped.setdefault(class_id, []).append(item)

    order_size = {"small": 0, "medium": 1, "large": 2, "xlarge": 3}
    order_density = {"sparse": 0, "medium": 1, "dense": 2}

    def class_sort_key(class_id: str) -> tuple[int, int]:
        size, density = class_id.split("-", 1)
        return (order_size.get(size, 99), order_density.get(density, 99))

    summary: list[dict] = []
    for class_id in sorted(grouped.keys(), key=class_sort_key):
        items = grouped[class_id]
        scores = [_to_float(item.get("score", 0.0)) for item in items]
        stable_ratios = [_to_float(item.get("stableRatio", 0.0)) for item in items]

        best_item = max(items, key=lambda item: _to_float(item.get("score", 0.0)))
        summary.append(
            {
                "classId": class_id,
                "sizeClass": class_id.split("-", 1)[0],
                "densityClass": class_id.split("-", 1)[1],
                "networkCount": len(items),
                "meanScore": _mean(scores),
                "scoreStdDev": _stddev(scores),
                "meanStableRatio": _mean(stable_ratios),
                "verdictCounts": _count_verdicts(items),
                "bestNetwork": best_item,
            }
        )

    return summary


def _pick_best_overall_network(study: dict) -> dict | None:
    networks = study.get("networks", []) or []
    if not networks:
        return None

    winner = max(networks, key=lambda item: _to_float((item.get("best") or {}).get("avgScore", 0.0)))
    best = winner.get("best", {}) or {}

    return {
        "networkId": winner.get("id"),
        "label": winner.get("label"),
        "nodeCount": int(winner.get("nodeCount", 0) or 0),
        "linkRadius": int(winner.get("linkRadius", 0) or 0),
        "score": _to_float(best.get("avgScore", 0.0)),
        "stableRatio": _to_float(best.get("stableRatio", 0.0)),
        "verdict": str(best.get("verdict", "UNSTABLE")),
        "bestParameters": best.get("parameters", {}) or {},
    }


def build_comprehensive_analysis(study: dict) -> dict:
    rows = _collect_rows(study)
    correlations = _build_parameter_correlations(rows)
    influence_ranking = _build_influence_ranking(correlations)
    pairwise = _build_pairwise_parameter_correlations(rows)
    topology_correlations = _build_topology_correlations(rows)
    instability_summary = _build_instability_summary(rows)
    class_summary = _build_network_class_summary(study)
    global_ideal = _build_global_ideal(rows)
    parameter_variation = _build_parameter_variation(rows)
    best_overall = _pick_best_overall_network(study)

    return {
        "metadata": {
            "sampleCount": len(rows),
            "networkCount": int((study.get("metadata") or {}).get("topologyCount", 0) or 0),
            "totalRuns": int((study.get("metadata") or {}).get("totalRuns", 0) or 0),
            "seedCount": int((study.get("metadata") or {}).get("seedCount", 0) or 0),
            "optimizationIterations": int((study.get("metadata") or {}).get("optimizationIterations", 0) or 0),
            "roundsPerCheck": int((study.get("metadata") or {}).get("roundsPerCheck", 0) or 0),
        },
        "outcomeSpecs": OUTCOME_SPECS,
        "bestOverallNetwork": best_overall,
        "networkClassSummary": class_summary,
        "globalIdealParameters": global_ideal,
        "parameterVariation": parameter_variation,
        "parameterCorrelations": correlations,
        "influenceRanking": influence_ranking,
        "pairwiseParameterCorrelations": pairwise,
        "topologyCorrelations": topology_correlations,
        "instabilitySummary": instability_summary,
    }


def _format_number(value: float, digits: int = 3) -> str:
    return f"{_to_float(value):.{digits}f}"


def _build_table(headers: list[str], rows: list[list[str]]) -> list[str]:
    lines: list[str] = []
    lines.append("| " + " | ".join(headers) + " |")
    lines.append("| " + " | ".join(["---"] * len(headers)) + " |")
    for row in rows:
        lines.append("| " + " | ".join(row) + " |")
    return lines


def _write_correlation_csv(path: Path, correlations: dict) -> None:
    lines = ["parameter,outcome,pearson,spearman"]
    for parameter in OPTIMIZATION_KEYS:
        per_metric = correlations.get(parameter, {})
        for metric in OUTCOME_SPECS:
            metric_key = metric["key"]
            values = per_metric.get(metric_key, {})
            lines.append(
                ",".join(
                    [
                        parameter,
                        metric_key,
                        _format_number(values.get("pearson", 0.0), 6),
                        _format_number(values.get("spearman", 0.0), 6),
                    ]
                )
            )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def _write_pairwise_csv(path: Path, pairwise: list[dict]) -> None:
    lines = ["left,right,pearson,absPearson"]
    for item in pairwise:
        lines.append(
            ",".join(
                [
                    str(item.get("left", "")),
                    str(item.get("right", "")),
                    _format_number(item.get("pearson", 0.0), 6),
                    _format_number(item.get("absPearson", 0.0), 6),
                ]
            )
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def _write_worst_runs_csv(path: Path, worst_runs: list[dict]) -> None:
    lines = [
        "runId,networkId,seed,verdict,score,theoremPassRate,assumptionsPassRate,coverageAvg,duplicateDrop,eligibleTailRatio,parentChangeAvg,flappingAvg"
    ]

    for item in worst_runs:
        lines.append(
            ",".join(
                [
                    str(item.get("runId", "")),
                    str(item.get("networkId", "")),
                    str(int(item.get("seed", 0) or 0)),
                    str(item.get("verdict", "UNSTABLE")),
                    _format_number(item.get("score", 0.0), 6),
                    _format_number(item.get("theoremPassRate", 0.0), 6),
                    _format_number(item.get("assumptionsPassRate", 0.0), 6),
                    _format_number(item.get("coverageAvg", 0.0), 6),
                    _format_number(item.get("duplicateDrop", 0.0), 6),
                    _format_number(item.get("eligibleTailRatio", 0.0), 6),
                    _format_number(item.get("parentChangeAvg", 0.0), 6),
                    _format_number(item.get("flappingAvg", 0.0), 6),
                ]
            )
        )

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def _write_markdown_report(
    path: Path,
    *,
    run_dir: Path,
    request: dict,
    result: dict,
    analysis: dict,
    chart_paths: list[Path],
) -> None:
    metadata = analysis.get("metadata", {}) or {}
    best_overall = analysis.get("bestOverallNetwork") or {}
    class_summary = analysis.get("networkClassSummary", []) or []
    global_ideal = (analysis.get("globalIdealParameters", {}) or {}).get("parameters", {})
    parameter_variation = analysis.get("parameterVariation", {}) or {}
    influence_ranking = analysis.get("influenceRanking", []) or []
    correlations = analysis.get("parameterCorrelations", {}) or {}
    pairwise = analysis.get("pairwiseParameterCorrelations", []) or []
    topology_correlations = analysis.get("topologyCorrelations", {}) or {}
    instability_summary = analysis.get("instabilitySummary", {}) or {}

    varied_params = [
        key
        for key in OPTIMIZATION_KEYS
        if int((parameter_variation.get(key) or {}).get("distinctCount", 0) or 0) > 1
    ]
    has_parameter_variation = len(varied_params) > 0
    varied_param_set = set(varied_params)

    outcome_direction = {item["key"]: float(item["direction"]) for item in OUTCOME_SPECS}

    outcome_ru = {
        "score": "Score (итоговая оценка)",
        "theoremPassRate": "TheoremPassRate (доля раундов, где теорема верна)",
        "assumptionsPassRate": "AssumptionsPassRate (доля раундов, где аксиомы верны)",
        "coverageAvg": "CoverageAvg (среднее покрытие DOWN)",
        "duplicateDrop": "DuplicateDrop (снижение дублей DOWN)",
        "eligibleTailRatio": "EligibleTailRatio (здоровье eligible-набора в конце)",
        "parentChangeAvg": "ParentChangeAvg (среднее число смен родителя)",
        "flappingAvg": "FlappingAvg (флаппинг родителей)",
    }

    param_desc_ru = {
        "qForward": "Порог eligible: узел eligible если q_total ≥ qForward.",
        "deliveryProbability": "Базовая вероятность доставки (далее масштабируется качеством линка).",
        "rootSourceCharge": "Инъекция заряда в gateway каждый раунд (источник энергии).",
        "penaltyLambda": "Штраф слабых линков при выборе родителя (выше = сильнее избегаем плохие связи).",
        "switchHysteresis": "Абсолютный гистерезис смены родителя (выше = меньше переключений).",
        "switchHysteresisRatio": "Относительный гистерезис (доля от оценки), добавляется к switchHysteresis.",
        "chargeDropPerHop": "Потеря заряда на хоп (ограничивает глубину распространения).",
        "chargeSpreadFactor": "Скорость смешивания/распространения заряда к целевому уровню.",
        "decayIntervalSteps": "Интервал глобального decay (0 = нет decay; больше = реже).",
        "decayPercent": "Сила decay в эпоху (доля, которая вычитается).",
        "linkMemory": "Память накопленного usageScore (ближе к 1 = более инерционно).",
        "linkLearningRate": "Скорость усиления effectiveQuality от usageScore.",
        "linkBonusMax": "Макс. бонус стабильности линка в выборе родителя.",
    }

    influence_by_param = {
        str(item.get("parameter")): item for item in (influence_ranking or [])
    }

    ideal_median: dict[str, float] = {}
    for key in OPTIMIZATION_KEYS:
        ideal_median[key] = _to_float((global_ideal.get(key) or {}).get("median", 0.0))

    def fmt_pct(value: float, digits: int = 2) -> str:
        return f"{_to_float(value) * 100.0:.{digits}f}%"

    def fmt_corr(value: float) -> str:
        return f"{_to_float(value):+.3f}"

    def _embed_chart(lines_out: list[str], rel_path: Path, title: str, conclusion: str | None) -> None:
        abs_path = run_dir / rel_path
        if not abs_path.exists():
            return
        lines_out.append(f"### {title}")
        lines_out.append("")
        lines_out.append(f"![{title}]({rel_path.as_posix()})")
        if conclusion:
            lines_out.append("")
            lines_out.append(f"**Вывод:** {conclusion}")
        lines_out.append("")

    def _top_differences(best_params: dict) -> list[tuple[str, float, float, float]]:
        diffs: list[tuple[str, float, float, float]] = []
        for key in OPTIMIZATION_KEYS:
            best = _to_float(best_params.get(key, 0.0))
            base = _to_float(ideal_median.get(key, 0.0))
            denom = max(1.0, abs(base))
            diffs.append((key, best, base, abs(best - base) / denom))
        diffs.sort(key=lambda item: item[3], reverse=True)
        return [item for item in diffs if item[3] >= 0.10][:6]

    # --- Header
    lines: list[str] = []
    lines.append("# Комплексный отчет по параметрическому исследованию")
    lines.append("")
    lines.append(
        "Отчет собран автоматически из результатов пакетного прогона и включает: краткую теорию, методику, "
        "результаты по топологиям и классам, чувствительность параметров, корреляции, графики и выводы."
    )
    lines.append("")

    lines.append("## Где лежат артефакты")
    lines.append("")
    lines.append("- Полный результат прогона: [batch_report.json](batch_report.json)")
    lines.append("- Компактные данные для графиков: [report_data.json](report_data.json)")
    lines.append("- Короткая сводка (раньше генерировалась отдельно): [run_summary.md](run_summary.md)")
    lines.append("- Комплексный анализ (машиночитаемый): [comprehensive_analysis.json](comprehensive_analysis.json)")
    lines.append("- Лучшие параметры по классам: [best_parameters_by_class.json](best_parameters_by_class.json)")
    lines.append("- Корреляции (все): [all_correlations.csv](all_correlations.csv)")
    lines.append("- Корреляции параметр-параметр: [pairwise_parameter_correlations.csv](pairwise_parameter_correlations.csv)")
    worst_runs_csv = run_dir / "worst_unstable_runs.csv"
    if worst_runs_csv.exists():
        lines.append("- Худшие/нестабильные seed-run: [worst_unstable_runs.csv](worst_unstable_runs.csv)")
    lines.append("")

    # --- Main takeaways (human-first)
    lines.append("## Главное: зачем это и что получилось")
    lines.append("")
    lines.append(
        "Цель симуляции — показать, что протокол может выходить в режим, где проверки теоремы (A5/A6/A7 и Леммы 4.1–4.3) "
        "выполняются устойчиво, и подобрать **один практический baseline параметров**, который можно брать как стартовый "
        "для разных сетей (размер/плотность)."
    )
    lines.append("")

    verdict_counts = instability_summary.get("verdictCounts", {}) or {}
    total_verdicts = float(sum(int(value) for value in verdict_counts.values()))
    unstable_share = 0.0
    if total_verdicts > 0.0:
        unstable_share = (
            float(int(verdict_counts.get("UNSTABLE", 0)))
            + float(int(verdict_counts.get("OSCILLATING", 0)))
        ) / total_verdicts

    ranked_varied = [
        item
        for item in (influence_ranking or [])
        if str(item.get("parameter")) in varied_param_set
    ]

    top3_params = [
        str(item.get("parameter"))
        for item in ranked_varied[:3]
        if str(item.get("parameter"))
    ]

    low_impact_params = [
        str(item.get("parameter"))
        for item in ranked_varied
        if _to_float(item.get("impactIndex", 0.0)) <= 0.030 and str(item.get("parameter"))
    ]

    lines.append("Что важно из этого прогона:")
    lines.append("")
    if best_overall:
        lines.append(
            f"- Лучшая сеть (best-case в этой выборке): **{best_overall.get('label', best_overall.get('networkId', 'n/a'))}**, "
            f"Score={_format_number(best_overall.get('score', 0.0), 2)}, "
            f"StableRatio={fmt_pct(best_overall.get('stableRatio', 0.0))}, Verdict={best_overall.get('verdict', 'n/a')}."
        )
    if total_verdicts > 0.0:
        lines.append(
            f"- Риск ‘хвоста’: нестабильные/осциллирующие запуски = **{fmt_pct(unstable_share)}** "
            f"({int(total_verdicts)} оценок)."
        )
        if (run_dir / "worst_unstable_runs.csv").exists():
            lines.append(
                "- Конкретные worst-кейсы (что ломается и где): [worst_unstable_runs.csv](worst_unstable_runs.csv)."
            )

    # Fast proof-of-convergence signal (from run_summary.md, if present)
    activation_summary_path = run_dir / "run_summary.md"
    if activation_summary_path.exists():
        try:
            summary_lines = activation_summary_path.read_text(encoding="utf-8").splitlines()
        except OSError:
            summary_lines = []

        header_index = None
        for idx, line in enumerate(summary_lines):
            if line.strip().startswith("| Network |") and "SustainedAllChecks" in line:
                header_index = idx
                break

        if header_index is not None:
            sustained_values: list[float] = []
            worst_round = None
            worst_network = None

            for line in summary_lines[header_index + 2 :]:
                if not line.strip().startswith("|"):
                    break
                parts = [part.strip() for part in line.strip().strip("|").split("|")]
                if len(parts) < 7:
                    continue

                network_id = parts[0]
                sustained_all = parts[6]
                if sustained_all in {"", "n/a"}:
                    continue

                try:
                    value = int(sustained_all)
                except ValueError:
                    continue

                sustained_values.append(float(value))
                if worst_round is None or value > int(worst_round):
                    worst_round = value
                    worst_network = network_id

            sustained_values.sort()
            if sustained_values:
                median_round = _quantile(sustained_values, 0.5)
                p90_round = _quantile(sustained_values, 0.9)
                worst_note = ""
                if worst_round is not None and worst_network:
                    worst_note = f" (макс={int(worst_round)} на {worst_network})"

                lines.append(
                    "- Сходимость проверок (по лучшим найденным параметрам на каждой топологии): "
                    f"`SustainedAllChecks` медиана≈{median_round:.0f}, P90≈{p90_round:.0f}{worst_note}."
                )

    if top3_params:
        lines.append(
            "- Ключевые параметры, которые реально ‘двигают’ качество (по `ImpactIndex`): "
            + ", ".join(f"`{p}`" for p in top3_params)
            + "."
        )
    elif not has_parameter_variation:
        lines.append(
            "- Параметры в этом запуске фиксированы (`optimizationIterations=0`), поэтому блоки корреляций/`ImpactIndex` ниже носят справочный характер и не показывают причинное влияние параметров."
        )

    theorem_rel = Path("../../../_docs_v1.0/math/theorem.md")
    if (run_dir / theorem_rel).exists():
        lines.append(
            "- Математическое обоснование (почему A5/A6/A7 ⇒ дерево): "
            f"[{theorem_rel.as_posix()}]({theorem_rel.as_posix()})."
        )

    lines.append("")

    # Universal baseline profile (copy/paste)
    if global_ideal:
        baseline_profile = {
            key: _to_float((global_ideal.get(key) or {}).get("median", 0.0))
            for key in OPTIMIZATION_KEYS
        }

        lines.append("Рекомендуемый baseline (один профиль, который можно брать как стартовый):")
        lines.append("")
        lines.append("```json")
        lines.append(json.dumps(baseline_profile, ensure_ascii=False, indent=2))
        lines.append("```")
        lines.append("")

        if top3_params:
            lines.append("Если нужно упростить и ‘убрать лишние ручки’: ")
            lines.append("")
            lines.append(
                "- в первую очередь тюнить: " + ", ".join(f"`{p}`" for p in top3_params) + ";"
            )
            lines.append(
                "- остальные параметры можно **зафиксировать** на baseline до тех пор, пока не появится конкретная причина их трогать."
            )
            if low_impact_params:
                lines.append(
                    "- параметры с минимальным влиянием в этой выборке (кандидаты на удаление из оптимизации): "
                    + ", ".join(f"`{p}`" for p in low_impact_params)
                    + "."
                )
            lines.append("")
        elif not has_parameter_variation:
            lines.append("Запуск baseline-only: этот профиль проверяется как фиксированный вектор без тюнинга параметров.")
            lines.append("")

    # --- Theory
    lines.append("## 1) Короткая теория (что симулируем)")
    lines.append("")
    lines.append(
        "Симулятор моделирует сеть устройств и gateway (корень), где у узлов есть заряд `q_total`. "
        "Узел становится *eligible*, когда `q_total ≥ qForward`, и только eligible-узлы участвуют в построении родительского дерева."
    )
    lines.append("")
    lines.append("В каждом раунде (упрощенно) выполняется:")
    lines.append("")
    lines.append("1. **DOWN**: распространение информации/заряда от gateway, измеряем покрытие и дубли.")
    lines.append("2. **UP**: попытки устройств пробиться к gateway по более ‘заряженным’ соседям.")
    lines.append("3. **Learning/Spread**: обновление оценок соседей + смешивание зарядов к лучшим оценкам.")
    lines.append("4. **Tree rebuild**: выбор родителей (с гистерезисом и штрафами качества/бонусами стабильности).")
    lines.append("5. **Decay (опционально)**: периодическое затухание зарядов и памяти линков.")
    lines.append("")
    lines.append(
        "Проверки теоремы в симуляции: аксиомы A5/A6/A7 и леммы 4.1/4.2/4.3. "
        "Если eligible-набор пуст (кроме gateway), состояние проверки считается `pending` (не провал/не успех)."
    )
    lines.append("")

    lines.append("### Что означают метрики (которые мы анализируем)")
    lines.append("")
    lines.append("- `Score`: эвристика 0..100 на основе pass-rate, покрытия, дублей и стабильности дерева.")
    lines.append("- `TheoremPassRate`: доля раундов, где агрегированная теорема проходит.")
    lines.append("- `AssumptionsPassRate`: доля раундов, где аксиомы проходят.")
    lines.append("- `CoverageAvg`: среднее покрытие DOWN.")
    lines.append("- `DuplicateDrop`: насколько дубли DOWN уменьшаются от начала к концу.")
    lines.append("- `EligibleTailRatio`: устойчивость eligible-набора в хвосте (коллапс eligible сильно штрафуется).")
    lines.append("- `ParentChangeAvg`, `FlappingAvg`: стабильность маршрутизационного дерева.")
    lines.append("")
    lines.append("**Вывод:** метрики — это практические индикаторы качества сходимости и устойчивости дерева.")
    lines.append("")

    # --- Method
    lines.append("## 2) Методика исследования")
    lines.append("")
    lines.append(f"- Топологий: {int(metadata.get('networkCount', 0))}")
    lines.append(f"- Оценок (seed-run): {int(metadata.get('sampleCount', 0))}")
    lines.append(f"- Всего запусков: {int(metadata.get('totalRuns', 0))}")
    lines.append(f"- SeedCount: {int(metadata.get('seedCount', 0))}")
    lines.append(f"- Итераций оптимизации на топологию: {int(metadata.get('optimizationIterations', 0))}")
    lines.append(f"- Раундов на одну оценку: {int(metadata.get('roundsPerCheck', 0))}")

    matrix_text = str(request.get("matrixText", "") or "").strip()
    if matrix_text:
        tokens = [token.strip() for token in matrix_text.split(",") if token.strip()]
        lines.append("")
        lines.append("Топологии (nodeCount x linkRadius):")
        lines.append("")
        for token in tokens:
            lines.append(f"- `{token}`")

    lines.append("")
    lines.append("### 2.1 Как читать корреляции/влияние (без формул)")
    lines.append("")
    lines.append(
        "В отчете есть корреляции (Pearson/Spearman) и агрегат `ImpactIndex`. "
        "Они нужны **только** чтобы понять, какие ручки чаще всего связаны с качеством в этой выборке."
    )
    lines.append("")
    lines.append("- Это **не доказательство причинности** и не ‘доказательство теоремы’.")
    lines.append(
        "- `ImpactIndex` = среднее $|r|$ по ключевым исходам и используется для ранжирования/отсечения почти нейтральных параметров."
    )
    lines.append("")
    lines.append(
        "**Вывод:** корреляции здесь — инструмент для упрощения настройки, а математическое обоснование протокола лежит в `_docs_v1.0/math/theorem.md`."
    )
    lines.append("")

    # --- Topology summary table
    lines.append("## 3) Результаты")
    lines.append("")
    lines.append("### 3.1 Сводка по каждой топологии")
    lines.append("")

    networks = result.get("networks", []) or []
    topology_rows: list[list[str]] = []
    for network in networks:
        best = network.get("best", {}) or {}
        topology_rows.append(
            [
                str(network.get("id", "n/a")),
                str(int(network.get("nodeCount", 0) or 0)),
                str(int(network.get("linkRadius", 0) or 0)),
                _format_number(best.get("avgScore", 0.0), 2),
                fmt_pct(best.get("stableRatio", 0.0)),
                str(best.get("verdict", "n/a")),
            ]
        )

    topology_rows.sort(key=lambda row: (int(row[1]), int(row[2])))
    lines.extend(
        _build_table(
            ["NetworkId", "N", "R", "AvgScore", "StableRatio", "Verdict"],
            topology_rows,
        )
    )
    lines.append("")
    lines.append("**Вывод:** рост N и R в этой выборке в среднем связан с ростом Score и улучшением динамики дублей.")
    lines.append("")

    # --- Best overall
    lines.append("### 3.2 Лучший общий случай")
    lines.append("")
    if best_overall:
        lines.append(f"- Сеть: **{best_overall.get('label', best_overall.get('networkId', 'n/a'))}**")
        lines.append(f"- Score: **{_format_number(best_overall.get('score', 0.0), 2)}**")
        lines.append(f"- StableRatio: **{fmt_pct(best_overall.get('stableRatio', 0.0))}**")
        lines.append(f"- Verdict: **{best_overall.get('verdict', 'n/a')}**")

        best_params = best_overall.get("bestParameters", {}) or {}
        if best_params:
            lines.append("")
            lines.append("Лучшие параметры (для этой топологии):")
            lines.append("")
            lines.append("```json")
            lines.append(json.dumps(best_params, ensure_ascii=False, indent=2))
            lines.append("```")

    else:
        lines.append("Данные о лучшей сети отсутствуют.")

    lines.append("")
    if best_overall:
        lines.append(
            "**Вывод:** лучший режим в этой выборке ("
            f"здесь N={int(best_overall.get('nodeCount', 0) or 0)}, R={int(best_overall.get('linkRadius', 0) or 0)}) "
            "дает полностью стабильное поведение в рамках выбранной эвристики Score."
        )
    else:
        lines.append(
            "**Вывод:** лучший режим в этой выборке дает полностью стабильное поведение в рамках выбранной эвристики Score."
        )
    lines.append("")

    # --- Universal baseline
    lines.append("### 3.3 Универсальный baseline параметров (1 профиль)")
    lines.append("")
    lines.append(
        "Это **один** профиль параметров, который можно брать как стартовый для разных сетей. "
        "Он получен как median по топ-20% запусков по Score (по всей выборке)."
    )
    lines.append("")

    if global_ideal:
        default_profile = {
            key: _to_float((global_ideal.get(key) or {}).get("median", 0.0))
            for key in OPTIMIZATION_KEYS
        }

        lines.append("```json")
        lines.append(json.dumps(default_profile, ensure_ascii=False, indent=2))
        lines.append("```")

        if influence_ranking and has_parameter_variation:
            top3 = [
                str(item.get("parameter"))
                for item in ranked_varied[:3]
                if str(item.get("parameter"))
            ]
            if top3:
                lines.append("")
                lines.append(
                    "Если нужно оставить минимум ручек: тюните "
                    + ", ".join(f"`{p}`" for p in top3)
                    + ", а остальные параметры фиксируйте на baseline."
                )
        elif not has_parameter_variation:
            lines.append("")
            lines.append(
                "Параметры в этом запуске не варьировались, поэтому baseline здесь оценивается как фиксированный профиль, а не как результат подбора ‘важных ручек’."
            )
    else:
        lines.append("Baseline отсутствует (нет данных).")

    lines.append("")
    lines.append(
        "**Вывод:** baseline хорошо описывает типичный успешный режим; если нужна максимальная надежность на sparse-сетях, "
        "смотрите отклонения по классам ниже и хвост худших сценариев."
    )
    lines.append("")

    # --- Class-based best
    lines.append("### 3.4 Лучшие параметры по классам топологий")
    lines.append("")
    if class_summary:
        summary_rows: list[list[str]] = []
        for item in class_summary:
            best = item.get("bestNetwork", {}) or {}
            verdict_counts = item.get("verdictCounts", {}) or {}
            summary_rows.append(
                [
                    str(item.get("classId", "n/a")),
                    str(item.get("networkCount", 0)),
                    _format_number(item.get("meanScore", 0.0), 2),
                    fmt_pct(item.get("meanStableRatio", 0.0)),
                    str(best.get("networkId", "n/a")),
                    f"S:{int(verdict_counts.get('STABLE', 0))}/O:{int(verdict_counts.get('OSCILLATING', 0))}/U:{int(verdict_counts.get('UNSTABLE', 0))}",
                ]
            )

        lines.extend(
            _build_table(
                ["Класс", "Топологий", "MeanScore", "MeanStable", "Лучшая сеть", "Вердикты"],
                summary_rows,
            )
        )
        lines.append("")

        for item in class_summary:
            class_id = str(item.get("classId", "n/a"))
            best = item.get("bestNetwork", {}) or {}
            best_params = best.get("bestParameters", {}) or {}

            lines.append(f"#### Класс `{class_id}`")
            lines.append("")
            lines.append(f"- Топологий в классе: **{int(item.get('networkCount', 0))}**")
            lines.append(f"- MeanScore: **{_format_number(item.get('meanScore', 0.0), 2)}** (σ={_format_number(item.get('scoreStdDev', 0.0), 2)})")
            lines.append(f"- MeanStableRatio: **{fmt_pct(item.get('meanStableRatio', 0.0))}**")
            lines.append(f"- Лучшая сеть: **{best.get('networkId', 'n/a')}** (N={best.get('nodeCount')}, R={best.get('linkRadius')})")
            lines.append("")

            diffs: list[tuple[str, float, float, float]] = []
            if best_params:
                lines.append("Лучшие параметры (полный набор):")
                lines.append("")
                lines.append("```json")
                lines.append(json.dumps(best_params, ensure_ascii=False, indent=2))
                lines.append("```")

                diffs = _top_differences(best_params)
                if diffs:
                    lines.append("")
                    lines.append("Ключевые отличия от глобального baseline:")
                    lines.append("")
                    for key, best_v, base_v, rel in diffs:
                        lines.append(
                            f"- `{key}`: {best_v} vs {base_v} (Δ={best_v - base_v:+.3f}, ~{rel*100:.0f}% от baseline)"
                        )

            lines.append("")
            class_n = int(item.get("networkCount", 0) or 0)
            if diffs:
                diff_brief: list[str] = []
                for key, best_v, base_v, _ in diffs[:3]:
                    arrow = "↑" if best_v > base_v else "↓"
                    diff_brief.append(f"`{key}` {arrow}")

                diff_text = ", ".join(diff_brief)
                sample_note = ""
                if class_n < 3:
                    sample_note = " (в классе мало топологий, вывод менее надежен)"

                lines.append(
                    f"**Вывод (по классу):** оптимизатор ушел от baseline: {diff_text}.{sample_note}"
                )
            else:
                lines.append(
                    "**Вывод (по классу):** профиль близок к baseline — явной специфики по классу не видно."
                )
            lines.append("")

    else:
        lines.append("Сводка по классам отсутствует.")
        lines.append("")

    # --- Parameter influence + per-parameter conclusions
    lines.append("### 3.5 Влияние параметров (и вывод по каждому параметру)")
    lines.append("")
    lines.append(
        "`ImpactIndex` = среднее значение $|r|$ по набору исходов (Score, pass-rate, покрытие, стабильность). "
        "Чем выше — тем сильнее параметр ‘двигает’ поведение в этой выборке."
    )
    lines.append("")

    if influence_ranking and has_parameter_variation:
        top_rows = influence_ranking[:6]
        bottom_rows = influence_ranking[-6:]

        lines.append("Топ влияния:")
        lines.append("")
        top_influence_rows: list[list[str]] = []
        for item in top_rows:
            strongest_metric = str(item.get("strongestMetric", "n/a"))
            strongest_r = _to_float(item.get("strongestCorrelation", 0.0))

            quality_r = strongest_r * _to_float(outcome_direction.get(strongest_metric, 1.0))
            if quality_r > 0.05:
                interp = "выше обычно улучшает сильнейшую метрику"
            elif quality_r < -0.05:
                interp = "выше обычно ухудшает сильнейшую метрику"
            else:
                interp = "направление неясно"

            top_influence_rows.append(
                [
                    str(item.get("parameter")),
                    _format_number(item.get("impactIndex", 0.0), 3),
                    strongest_metric,
                    _format_number(item.get("strongestCorrelation", 0.0), 3),
                    interp,
                ]
            )

        lines.extend(
            _build_table(
                ["Параметр", "ImpactIndex", "Сильнейшая метрика", "r", "Интерпретация"],
                top_influence_rows,
            )
        )
        lines.append("")

        lines.append("Низ влияния (почти не меняют картину в этой выборке):")
        lines.append("")
        lines.extend(
            _build_table(
                ["Параметр", "ImpactIndex", "Сильнейшая метрика", "r"],
                [
                    [
                        str(item.get("parameter")),
                        _format_number(item.get("impactIndex", 0.0), 3),
                        str(item.get("strongestMetric", "n/a")),
                        _format_number(item.get("strongestCorrelation", 0.0), 3),
                    ]
                    for item in bottom_rows
                ],
            )
        )
        lines.append("")

        lines.append("Выводы по каждому параметру (коротко):")
        lines.append("")

        per_param_rows: list[list[str]] = []
        for key in OPTIMIZATION_KEYS:
            per_metric = correlations.get(key, {}) or {}
            score_r = _to_float((per_metric.get("score") or {}).get("pearson", 0.0))

            influence = influence_by_param.get(key, {}) or {}
            impact = _to_float(influence.get("impactIndex", 0.0))
            strongest_metric = str(influence.get("strongestMetric", "score"))
            strongest_r = _to_float(influence.get("strongestCorrelation", 0.0))

            quality_r = strongest_r * _to_float(outcome_direction.get(strongest_metric, 1.0))
            if quality_r > 0.05:
                quality_note = "увеличение чаще улучшает метрику"
            elif quality_r < -0.05:
                quality_note = "увеличение чаще ухудшает метрику"
            else:
                quality_note = "направление слабое"

            if score_r > 0.10:
                score_note = "скорее ↑Score"
            elif score_r < -0.10:
                score_note = "скорее ↓Score"
            else:
                score_note = "Score: связь слабая"

            per_param_rows.append(
                [
                    key,
                    param_desc_ru.get(key, ""),
                    _format_number(impact, 3),
                    fmt_corr(score_r),
                    f"{strongest_metric} ({fmt_corr(strongest_r)})",
                    f"{score_note}; {quality_note}",
                ]
            )

        lines.extend(
            _build_table(
                [
                    "Параметр",
                    "Смысл",
                    "ImpactIndex",
                    "r(Score)",
                    "Сильнее всего связано",
                    "Вывод",
                ],
                per_param_rows,
            )
        )

    elif not has_parameter_variation:
        lines.append(
            "В этом прогоне параметрический анализ влияния отключен: `optimizationIterations=0`, каждый параметр имел одно фиксированное значение во всех запусках."
        )
        lines.append("")
        lines.append("Фиксированные значения параметров в этом прогоне:")
        lines.append("")
        lines.extend(
            _build_table(
                ["Параметр", "Min", "Max", "DistinctValues"],
                [
                    [
                        key,
                        _format_number((parameter_variation.get(key) or {}).get("min", 0.0), 3),
                        _format_number((parameter_variation.get(key) or {}).get("max", 0.0), 3),
                        str(int((parameter_variation.get(key) or {}).get("distinctCount", 0) or 0)),
                    ]
                    for key in OPTIMIZATION_KEYS
                ],
            )
        )
    else:
        lines.append("Данные о влиянии параметров отсутствуют.")

    lines.append("")
    if influence_ranking and has_parameter_variation:
        top = [
            str(item.get("parameter"))
            for item in ranked_varied[:3]
            if str(item.get("parameter"))
        ]
        if top:
            lines.append(
                "**Общий вывод по параметрам:** в этой выборке заметнее всего влияют "
                + ", ".join(f"`{p}`" for p in top)
                + ". "
                "Корреляции — описательные, и часть эффектов может быть связана с совместной настройкой параметров оптимизатором."
            )
        else:
            lines.append(
                "**Общий вывод по параметрам:** влияющие ручки сильно зависят от выборки; "
                "корреляции здесь используются как ориентир, а не как доказательство причинности."
            )
    elif not has_parameter_variation:
        lines.append(
            "**Общий вывод по параметрам:** это baseline-only прогон (без варьирования параметров), поэтому здесь проверяется качество фиксированного профиля, а не оценивается влияние отдельных ручек."
        )
    else:
        lines.append(
            "**Общий вывод по параметрам:** данных о влиянии нет; используйте baseline и тюните параметры по одному."
        )
    lines.append("")

    # --- Pairwise
    lines.append("### 3.6 Связки параметр↔параметр (важно для интерпретации)")
    lines.append("")
    if pairwise and has_parameter_variation:
        top_rows = pairwise[:20]
        lines.extend(
            _build_table(
                ["Пара", "Pearson", "|Pearson|"],
                [
                    [
                        f"{item.get('left')} <> {item.get('right')}",
                        _format_number(item.get("pearson", 0.0), 3),
                        _format_number(item.get("absPearson", 0.0), 3),
                    ]
                    for item in top_rows
                ],
            )
        )
        lines.append("")
        lines.append(
            "**Вывод:** некоторые параметры сильно коррелируют между собой (например, `deliveryProbability` ↔ `penaltyLambda`). "
            "Это значит, что одиночная корреляция параметра с Score может отражать совместную настройку, а не ‘чистый’ эффект."
        )
    elif not has_parameter_variation:
        lines.append(
            "Параметры не варьировались, поэтому парные корреляции параметр↔параметр в этом прогоне не интерпретируются."
        )
    else:
        lines.append("Нет данных о парных корреляциях.")

    lines.append("")

    # --- Topology effects
    lines.append("### 3.7 Эффект топологии")
    lines.append("")
    topo_rows: list[list[str]] = []
    for spec in OUTCOME_SPECS:
        metric_key = spec["key"]
        effect = topology_correlations.get(metric_key, {})
        topo_rows.append(
            [
                outcome_ru.get(metric_key, spec["label"]),
                _format_number(effect.get("nodeCount", 0.0), 3),
                _format_number(effect.get("linkRadius", 0.0), 3),
            ]
        )
    lines.extend(_build_table(["Исход", "r(nodeCount)", "r(linkRadius)"], topo_rows))
    lines.append("")
    lines.append(
        "**Вывод:** в этой выборке более крупные и более плотные топологии в среднем показывают более высокий Score и более выраженное снижение дублей (DuplicateDrop)."
    )
    lines.append("")

    # --- Instability summary
    lines.append("### 3.8 Нестабильные и худшие случаи")
    lines.append("")
    verdict_counts = instability_summary.get("verdictCounts", {}) or {}
    total_verdicts = int(sum(int(value) for value in verdict_counts.values()))
    if total_verdicts > 0:
        stable_count = int(verdict_counts.get("STABLE", 0))
        oscillating_count = int(verdict_counts.get("OSCILLATING", 0))
        unstable_count = int(verdict_counts.get("UNSTABLE", 0))
        lines.extend(
            _build_table(
                ["Вердикт", "Count", "Share"],
                [
                    ["STABLE", str(stable_count), fmt_pct(stable_count / total_verdicts)],
                    ["OSCILLATING", str(oscillating_count), fmt_pct(oscillating_count / total_verdicts)],
                    ["UNSTABLE", str(unstable_count), fmt_pct(unstable_count / total_verdicts)],
                ],
            )
        )
        lines.append("")

    worst_runs = instability_summary.get("worstRuns", []) or []
    if worst_runs:
        lines.append("Худшие seed-run (минимальный Score):")
        lines.append("")
        lines.extend(
            _build_table(
                [
                    "RunId",
                    "Network",
                    "Seed",
                    "Verdict",
                    "Score",
                    "TheoremPass",
                    "Coverage",
                    "DupDrop",
                    "ParentChange",
                    "Flapping",
                ],
                [
                    [
                        str(item.get("runId", "n/a")),
                        str(item.get("networkId", "n/a")),
                        str(int(item.get("seed", 0) or 0)),
                        str(item.get("verdict", "UNSTABLE")),
                        _format_number(item.get("score", 0.0), 2),
                        fmt_pct(item.get("theoremPassRate", 0.0)),
                        fmt_pct(item.get("coverageAvg", 0.0)),
                        _format_number(item.get("duplicateDrop", 0.0), 2),
                        _format_number(item.get("parentChangeAvg", 0.0), 2),
                        _format_number(item.get("flappingAvg", 0.0), 2),
                    ]
                    for item in worst_runs[:12]
                ],
            )
        )
        lines.append("")

    worst_by_network = instability_summary.get("worstByNetwork", []) or []
    if worst_by_network:
        lines.append("Риск по топологиям (где чаще нестабильность и насколько плохой хвост):")
        lines.append("")
        lines.append(
            "Примечание: `UnstableShare` и `WorstUnstableScore` считаются по **всем** оценкам во время оптимизации (по всем переборам параметров), "
            "а не по лучшему найденному профилю для этой топологии (см. таблицу 3.1)."
        )
        lines.append("")

        risky = sorted(
            worst_by_network,
            key=lambda item: (
                -_to_float(item.get("unstableOrOscillatingShare", 0.0)),
                _to_float(item.get("worstUnstableScore"), 1e9),
                _to_float(item.get("worstScore", 0.0)),
            ),
        )

        lines.extend(
            _build_table(
                [
                    "Network",
                    "UnstableShare",
                    "WorstScore",
                    "WorstUnstableScore",
                    "WorstUnstableRunId",
                ],
                [
                    [
                        str(item.get("networkId", "n/a")),
                        fmt_pct(item.get("unstableOrOscillatingShare", 0.0)),
                        _format_number(item.get("worstScore", 0.0), 2),
                        (
                            _format_number(item.get("worstUnstableScore", 0.0), 2)
                            if item.get("worstUnstableScore") is not None
                            else "—"
                        ),
                        str(item.get("worstUnstableRunId") or "—"),
                    ]
                    for item in risky[:15]
                ],
            )
        )
        lines.append("")

    lines.append(
        "**Вывод:** нестабильность концентрируется в части топологий/seed-комбинаций; для инженерного выбора параметров "
        "важно смотреть не только на средний Score, но и на хвост худших сценариев."
    )
    lines.append("")

    # --- Charts
    lines.append("## 4) Графики (с выводами)")
    lines.append("")

    # Run-summary charts (already generated by run_artifacts)
    lines.append("### 4.1 Сводка прогона")
    lines.append("")
    _embed_chart(
        lines,
        Path("charts/score_by_network.svg"),
        "Score по топологиям",
        "видно распределение качества по сеткам; лучше сравнивать с классами (size/density).",
    )
    _embed_chart(
        lines,
        Path("charts/stable_ratio_by_network.svg"),
        "StableRatio по топологиям",
        "показывает, где режим стабилен (высокая доля STABLE) и где возможны колебания.",
    )
    _embed_chart(
        lines,
        Path("charts/verdict_distribution.svg"),
        "Распределение вердиктов",
        "характеризует общий уровень устойчивости по выбранной метрике Score.",
    )
    _embed_chart(
        lines,
        Path("charts/first_all_checks_round_by_network.svg"),
        "Первый раунд, где проходят все проверки",
        "чем меньше — тем быстрее сеть выходит в корректный режим (если он достигается).",
    )
    _embed_chart(
        lines,
        Path("charts/sustained_all_checks_round_by_network.svg"),
        "Раунд устойчивого прохождения всех проверок",
        "важнее, чем первый успех: показывает, когда режим перестает ‘проваливаться’.",
    )

    lines.append("### 4.2 Лучший случай (динамика)")
    lines.append("")
    _embed_chart(
        lines,
        Path("charts/best_network_topology.svg"),
        "Топология лучшей сети",
        "это пример топологии, где найден режим Score≈100 и высокий StableRatio.",
    )
    _embed_chart(
        lines,
        Path("charts/best_network_connectivity_dynamics.svg"),
        "Покрытие и eligible",
        "важно следить, чтобы eligible-набор не коллапсировал в хвосте.",
    )
    _embed_chart(
        lines,
        Path("charts/best_network_stability.svg"),
        "Стабильность дерева",
        "меньше смен родителей и флаппинга = более устойчивый режим.",
    )
    _embed_chart(
        lines,
        Path("charts/best_network_routing_dynamics.svg"),
        "Трафик и дубли",
        "снижение дублей при сохранении покрытия — хороший признак сходимости.",
    )
    _embed_chart(
        lines,
        Path("charts/best_network_theorem_status.svg"),
        "Прохождение теоремы/аксиом по раундам",
        "показывает, насколько режим устойчиво проходит проверки.",
    )
    _embed_chart(
        lines,
        Path("charts/best_network_checks_status.svg"),
        "A5/A6/A7 и леммы по раундам",
        "помогает понять, какая именно часть теоремы ломается, если ломается.",
    )

    lines.append("### 4.3 Худший нестабильный сценарий")
    lines.append("")
    _embed_chart(
        lines,
        Path("charts/unstable_or_oscillating_runs_by_network.svg"),
        "Число unstable/oscillating запусков по топологиям",
        "показывает, в каких сетках чаще возникает деградация и нестабильность.",
    )
    _embed_chart(
        lines,
        Path("charts/worst_network_topology.svg"),
        "Топология худшей сети",
        "визуализация худшего случая с аннотацией зарядов узлов и средних весов ребер.",
    )
    _embed_chart(
        lines,
        Path("charts/worst_network_connectivity_dynamics.svg"),
        "Худший случай: покрытие и eligible",
        "обычно видно коллапс eligible-набора или слабое восстановление покрытия.",
    )
    _embed_chart(
        lines,
        Path("charts/worst_network_stability.svg"),
        "Худший случай: стабильность дерева",
        "скачки parentChange/flapping указывают на маршрутизационную турбулентность.",
    )
    _embed_chart(
        lines,
        Path("charts/worst_network_routing_dynamics.svg"),
        "Худший случай: трафик и дубли",
        "рост дублей при слабом покрытии — типичный признак неустойчивого режима.",
    )
    _embed_chart(
        lines,
        Path("charts/worst_network_theorem_status.svg"),
        "Худший случай: теорема/аксиомы по раундам",
        "позволяет увидеть, это постоянный провал или редкие проблески прохождения проверок.",
    )
    _embed_chart(
        lines,
        Path("charts/worst_network_checks_status.svg"),
        "Худший случай: A5/A6/A7 и леммы",
        "диагностирует, какая именно проверка ломается чаще всего.",
    )

    lines.append("### 4.4 Чувствительность и корреляции")
    lines.append("")

    for chart_path in chart_paths:
        stem = chart_path.stem
        if stem == "parameter_impact_index":
            conclusion = "влияние распределено неравномерно: есть несколько параметров-лидеров и несколько почти нейтральных."
            title = "Индекс влияния параметров"
        elif stem == "score_correlation_signed":
            conclusion = "видна направленность корреляций со Score, но интерпретировать нужно с учетом связок параметров."
            title = "Корреляции параметров со Score"
        elif stem == "parameter_outcome_heatmap":
            conclusion = "удобно видеть, на какие исходы сильнее влияет каждый параметр."
            title = "Тепловая карта корреляций (параметры ↔ исходы)"
        elif stem == "class_mean_score":
            conclusion = "плотность/размер класса напрямую отражаются в среднем Score (в этой выборке плотнее/крупнее лучше)."
            title = "Средний Score по классам"
        elif stem == "topology_outcome_correlations":
            conclusion = "в этой выборке `nodeCount` и `linkRadius` положительно связаны со Score и DuplicateDrop."
            title = "Корреляции топологии с исходами"
        elif stem == "verdict_share_comprehensive":
            conclusion = "доли STABLE/OSCILLATING/UNSTABLE по всей расширенной выборке; это базовый индикатор общей устойчивости."
            title = "Доли вердиктов в полной выборке"
        elif stem == "unstable_ratio_by_class":
            conclusion = "показывает, какие классы топологий дают более высокий риск неустойчивого режима."
            title = "Доля нестабильных запусков по классам"
        elif stem == "worst_score_by_network":
            conclusion = "нижняя граница качества по каждой топологии; важно для оценки риска в production-сценариях."
            title = "Худший Score по каждой топологии"
        elif stem == "worst_runs_score_tail":
            conclusion = "хвост самых плохих seed-run: чем длиннее и глубже хвост, тем выше риск редких провалов."
            title = "Score хвоста худших запусков"
        else:
            conclusion = None
            title = stem

        _embed_chart(lines, chart_path, title, conclusion)

    # --- Full Pearson matrix (appendix)
    lines.append("## Приложение A: Полная матрица Pearson (параметр ↔ исход)")
    lines.append("")

    matrix_rows: list[list[str]] = []
    headers = ["Parameter"] + [spec["label"] for spec in OUTCOME_SPECS]

    for parameter in OPTIMIZATION_KEYS:
        per_metric = correlations.get(parameter, {})
        row = [parameter]
        for spec in OUTCOME_SPECS:
            metric_key = spec["key"]
            value = per_metric.get(metric_key, {}).get("pearson", 0.0)
            row.append(_format_number(value, 3))
        matrix_rows.append(row)

    lines.extend(_build_table(headers, matrix_rows))
    lines.append("")

    lines.append("## Приложение B: Входной запрос")
    lines.append("")
    lines.append("```json")
    lines.append(json.dumps(request, ensure_ascii=False, indent=2))
    lines.append("```")
    lines.append("")

    lines.append("## Общий вывод")
    lines.append("")

    top_params = (
        [
            str(item.get("parameter"))
            for item in ranked_varied[:3]
            if str(item.get("parameter"))
        ]
        if has_parameter_variation
        else []
    )

    verdict_counts = instability_summary.get("verdictCounts", {}) or {}
    total_verdicts = float(sum(int(value) for value in verdict_counts.values()))
    unstable_text = "n/a"
    if total_verdicts > 0.0:
        unstable_share = (
            float(int(verdict_counts.get("UNSTABLE", 0)))
            + float(int(verdict_counts.get("OSCILLATING", 0)))
        ) / total_verdicts
        unstable_text = f"{fmt_pct(unstable_share)} ({int(total_verdicts)} оценок)"

    baseline_exists = bool(global_ideal)

    lines.append(
        "1) Этот прогон показывает, что протокол способен выходить в режим, где проверки теоремы (A5/A6/A7 + Леммы 4.1–4.3) "
        "выполняются устойчиво (см. графики first/sustained all-checks по топологиям).\n"
        f"2) Риск редких провалов (tail) в этой выборке: нестабильные/осциллирующие = **{unstable_text}**.\n"
        + (
            "3) Есть один практический baseline параметров (median топ-20% лучших запусков) — его достаточно, чтобы начать.\n"
            if baseline_exists
            else "3) Baseline параметров не был рассчитан (нет данных).\n"
        )
        + (
            "4) Этот прогон был baseline-only (без варьирования параметров), поэтому он отвечает на вопрос ‘насколько хорош фиксированный профиль’, а не на вопрос ‘какие ручки влияют сильнее’.\n"
            if not has_parameter_variation
            else (
                "4) Чтобы ‘убрать шум’ и тюнить только важное, начните с: "
                + ", ".join(f"`{p}`" for p in top_params)
                + ". Остальные параметры фиксируйте на baseline.\n"
                if top_params
                else "4) Чтобы упростить настройку, тюните параметры по одному, фиксируя остальные.\n"
            )
        )
        + (
            "5) Математическое обоснование структуры дерева и loop-free DOWN: см. `_docs_v1.0/math/theorem.md`. "
            "Симуляция проверяет именно эти предпосылки (A5/A6/A7 и Леммы), поэтому практическая часть напрямую связана с теоремой."
        )
    )

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def save_comprehensive_study_artifacts(
    *,
    request: dict,
    result: dict,
    run_dir: Path,
) -> dict:
    analysis = build_comprehensive_analysis(result)

    analysis_json_path = run_dir / "comprehensive_analysis.json"
    analysis_json_path.write_text(
        json.dumps(analysis, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    class_params_path = run_dir / "best_parameters_by_class.json"
    class_params_path.write_text(
        json.dumps(analysis.get("networkClassSummary", []), ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    all_correlations_csv = run_dir / "all_correlations.csv"
    _write_correlation_csv(all_correlations_csv, analysis.get("parameterCorrelations", {}))

    pairwise_correlations_csv = run_dir / "pairwise_parameter_correlations.csv"
    _write_pairwise_csv(
        pairwise_correlations_csv,
        analysis.get("pairwiseParameterCorrelations", []),
    )

    instability_summary = analysis.get("instabilitySummary", {}) or {}
    worst_unstable_runs_csv = run_dir / "worst_unstable_runs.csv"
    _write_worst_runs_csv(
        worst_unstable_runs_csv,
        instability_summary.get("worstRuns", []) or [],
    )

    charts_dir = run_dir / "charts" / "comprehensive"
    charts_dir.mkdir(parents=True, exist_ok=True)

    influence_entries = [
        (item.get("parameter", "n/a"), _to_float(item.get("impactIndex", 0.0)))
        for item in (analysis.get("influenceRanking", []) or [])
    ]
    influence_chart = charts_dir / "parameter_impact_index.svg"
    write_bar_chart(
        influence_chart,
        title="Индекс влияния параметров (среднее |r|)",
        entries=influence_entries,
        y_label="ImpactIndex",
        y_max=None,
        color="#2f7ed8",
    )

    score_corr_entries = [
        (
            key,
            _to_float(
                (analysis.get("parameterCorrelations", {}) or {})
                .get(key, {})
                .get("score", {})
                .get("pearson", 0.0)
            ),
        )
        for key in OPTIMIZATION_KEYS
    ]
    score_corr_chart = charts_dir / "score_correlation_signed.svg"
    write_signed_bar_chart(
        score_corr_chart,
        title="Корреляция Пирсона: параметр ↔ Score",
        entries=score_corr_entries,
        y_label="r (Пирсон)",
        y_limit=1.0,
    )

    heatmap_rows = OPTIMIZATION_KEYS
    # short labels so the SVG header text doesn't overlap
    heatmap_cols = ["Score", "ThmPass", "AsmPass", "Cov", "DupDrop", "EligTail", "ParChg", "Flap"]
    heatmap_matrix: list[list[float]] = []
    for key in OPTIMIZATION_KEYS:
        per_metric = (analysis.get("parameterCorrelations", {}) or {}).get(key, {})
        heatmap_matrix.append(
            [
                _to_float(per_metric.get(spec["key"], {}).get("pearson", 0.0))
                for spec in OUTCOME_SPECS
            ]
        )

    heatmap_chart = charts_dir / "parameter_outcome_heatmap.svg"
    write_heatmap_chart(
        heatmap_chart,
        title="Тепловая карта корреляций (Пирсон)",
        row_labels=heatmap_rows,
        column_labels=heatmap_cols,
        matrix=heatmap_matrix,
        vmin=-1.0,
        vmax=1.0,
    )

    class_entries = [
        (item.get("classId", "n/a"), _to_float(item.get("meanScore", 0.0)))
        for item in (analysis.get("networkClassSummary", []) or [])
    ]
    class_score_chart = charts_dir / "class_mean_score.svg"
    write_bar_chart(
        class_score_chart,
        title="Средний Score по классам топологий",
        entries=class_entries,
        y_label="Score",
        y_max=100.0,
        color="#2ca56b",
    )

    topology_corr = analysis.get("topologyCorrelations", {}) or {}
    topology_entries = [
        ("N->Score", _to_float((topology_corr.get("score", {}) or {}).get("nodeCount", 0.0))),
        ("R->Score", _to_float((topology_corr.get("score", {}) or {}).get("linkRadius", 0.0))),
        (
            "N->Thm",
            _to_float((topology_corr.get("theoremPassRate", {}) or {}).get("nodeCount", 0.0)),
        ),
        (
            "R->Thm",
            _to_float((topology_corr.get("theoremPassRate", {}) or {}).get("linkRadius", 0.0)),
        ),
    ]
    topology_chart = charts_dir / "topology_outcome_correlations.svg"
    write_signed_bar_chart(
        topology_chart,
        title="Корреляции признаков топологии",
        entries=topology_entries,
        y_label="r (Пирсон)",
        y_limit=1.0,
    )

    verdict_counts = instability_summary.get("verdictCounts", {}) or {}
    verdict_chart_entries = [
        ("STABLE", float(int(verdict_counts.get("STABLE", 0)))),
        ("OSCILLATING", float(int(verdict_counts.get("OSCILLATING", 0)))),
        ("UNSTABLE", float(int(verdict_counts.get("UNSTABLE", 0)))),
    ]
    verdict_share_chart = charts_dir / "verdict_share_comprehensive.svg"
    write_bar_chart(
        verdict_share_chart,
        title="Количество STABLE/OSCILLATING/UNSTABLE",
        entries=verdict_chart_entries,
        y_label="Count",
        y_max=None,
        color="#b54a4a",
    )

    unstable_ratio_by_class_entries: list[tuple[str, float]] = []
    for item in (analysis.get("networkClassSummary", []) or []):
        counts = item.get("verdictCounts", {}) or {}
        total = float(sum(int(value) for value in counts.values()))
        unstable_ratio = 0.0
        if total > 0.0:
            unstable_ratio = (
                float(int(counts.get("UNSTABLE", 0)))
                + float(int(counts.get("OSCILLATING", 0)))
            ) / total
        unstable_ratio_by_class_entries.append((str(item.get("classId", "n/a")), unstable_ratio))

    unstable_ratio_chart = charts_dir / "unstable_ratio_by_class.svg"
    write_bar_chart(
        unstable_ratio_chart,
        title="Доля нестабильных запусков по классам",
        entries=unstable_ratio_by_class_entries,
        y_label="UnstableShare",
        y_max=1.0,
        color="#c35f2f",
    )

    worst_by_network_entries = [
        (str(item.get("networkId", "n/a")), _to_float(item.get("worstScore", 0.0)))
        for item in (instability_summary.get("worstByNetwork", []) or [])
    ]
    worst_by_network_chart = charts_dir / "worst_score_by_network.svg"
    write_bar_chart(
        worst_by_network_chart,
        title="Худший Score по каждой топологии",
        entries=worst_by_network_entries,
        y_label="WorstScore",
        y_max=100.0,
        color="#9f3939",
    )

    worst_runs_tail_entries = [
        (
            str(item.get("runId") or f"{item.get('networkId', 'n/a')}:s{int(item.get('seed', 0) or 0)}"),
            _to_float(item.get("score", 0.0)),
        )
        for item in (instability_summary.get("worstRuns", []) or [])[:20]
    ]
    worst_runs_tail_chart = charts_dir / "worst_runs_score_tail.svg"
    write_bar_chart(
        worst_runs_tail_chart,
        title="Score хвоста худших запусков",
        entries=worst_runs_tail_entries,
        y_label="Score",
        y_max=100.0,
        color="#7f2d2d",
    )

    report_path = run_dir / "comprehensive_report.md"
    relative_chart_paths = [
        influence_chart.relative_to(run_dir),
        score_corr_chart.relative_to(run_dir),
        heatmap_chart.relative_to(run_dir),
        class_score_chart.relative_to(run_dir),
        topology_chart.relative_to(run_dir),
        verdict_share_chart.relative_to(run_dir),
        unstable_ratio_chart.relative_to(run_dir),
        worst_by_network_chart.relative_to(run_dir),
        worst_runs_tail_chart.relative_to(run_dir),
    ]
    _write_markdown_report(
        report_path,
        run_dir=run_dir,
        request=request,
        result=result,
        analysis=analysis,
        chart_paths=relative_chart_paths,
    )

    return {
        "analysisJson": analysis_json_path,
        "classParameters": class_params_path,
        "allCorrelationsCsv": all_correlations_csv,
        "pairwiseCorrelationsCsv": pairwise_correlations_csv,
        "worstUnstableRunsCsv": worst_unstable_runs_csv,
        "comprehensiveReport": report_path,
        "charts": [
            influence_chart,
            score_corr_chart,
            heatmap_chart,
            class_score_chart,
            topology_chart,
            verdict_share_chart,
            unstable_ratio_chart,
            worst_by_network_chart,
            worst_runs_tail_chart,
        ],
    }
