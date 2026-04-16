"""Purpose: Save batch outputs into timestamped run directories with JSON, charts, and markdown summary."""

from __future__ import annotations

import json
from datetime import datetime
from pathlib import Path

from src.research.report_data_builder import build_report_data
from src.research.stability_scorer import score_stability
from src.research.svg_charts import (
    write_bar_chart,
    write_line_chart,
    write_network_topology_chart,
)
from src.state.no_ui_simulation_runner import run_no_ui_simulation

CHECK_ORDER = [
    "assumptionsPass",
    "theoremPass",
    "a5",
    "a6",
    "a7",
    "lemma41",
    "lemma42",
    "lemma43",
]


def _format_timestamp(value: datetime) -> str:
    return value.strftime("%Y-%m-%d_%H-%M-%S")


def _ensure_unique_run_dir(base_root: Path, started_at: datetime) -> Path:
    base_root.mkdir(parents=True, exist_ok=True)
    base_name = _format_timestamp(started_at)

    candidate = base_root / base_name
    suffix = 1
    while candidate.exists():
        candidate = base_root / f"{base_name}_{suffix:02d}"
        suffix += 1

    candidate.mkdir(parents=True, exist_ok=False)
    return candidate


def _build_verdict_counts(recommendations: list[dict]) -> list[tuple[str, float]]:
    counts = {
        "STABLE": 0.0,
        "OSCILLATING": 0.0,
        "UNSTABLE": 0.0,
    }
    for row in recommendations:
        verdict = str(row.get("verdict", "UNSTABLE"))
        counts[verdict] = counts.get(verdict, 0.0) + 1.0
    return [("STABLE", counts.get("STABLE", 0.0)), ("OSCILLATING", counts.get("OSCILLATING", 0.0)), ("UNSTABLE", counts.get("UNSTABLE", 0.0))]


def _pick_best_network(network_details: list[dict]) -> dict | None:
    if not network_details:
        return None
    return max(network_details, key=lambda item: float(item.get("bestAvgScore", 0)))


def _edge_key(a: int, b: int) -> str:
    return f"{a}:{b}" if a < b else f"{b}:{a}"


def _compact_topology_from_state(state: dict) -> dict:
    return {
        "nodes": [
            {
                "id": node.id,
                "x": float(node.x),
                "y": float(node.y),
                "isGateway": bool(node.is_gateway),
            }
            for node in state["nodes"].values()
        ],
        "edges": [
            {"a": int(edge["a"]), "b": int(edge["b"])}
            for edge in (state.get("edges") or [])
        ],
    }


def _build_chart_series_from_snapshots(snapshots: list[dict]) -> dict:
    source = snapshots or []
    return {
        "duplicates": [float(item.get("downDuplicates", 0)) for item in source],
        "coveragePercent": [float(item.get("downCoverage", 0)) * 100 for item in source],
        "eligibleCount": [float(item.get("eligibleCount", 0)) for item in source],
        "parentChanges": [float(item.get("parentChanges", 0)) for item in source],
        "flappingNodes": [float(item.get("flappingNodes", 0)) for item in source],
        "upHops": [float(item.get("upHops", 0)) for item in source],
        "upUpdates": [float(item.get("upUpdates", 0)) for item in source],
        "propagationDeliveries": [float(item.get("propagationDeliveries", 0)) for item in source],
        "spreadUpdates": [float(item.get("spreadUpdates", 0)) for item in source],
        "assumptionsPassBinary": [1.0 if item.get("assumptionsPass") is True else 0.0 for item in source],
        "theoremPassBinary": [1.0 if item.get("theoremPass") is True else 0.0 for item in source],
        "allChecksBinary": [
            1.0 if all(item.get(key) is True for key in CHECK_ORDER[2:]) else 0.0
            for item in source
        ],
        "a5Binary": [1.0 if item.get("a5") is True else 0.0 for item in source],
        "a6Binary": [1.0 if item.get("a6") is True else 0.0 for item in source],
        "a7Binary": [1.0 if item.get("a7") is True else 0.0 for item in source],
        "lemma41Binary": [1.0 if item.get("lemma41") is True else 0.0 for item in source],
        "lemma42Binary": [1.0 if item.get("lemma42") is True else 0.0 for item in source],
        "lemma43Binary": [1.0 if item.get("lemma43") is True else 0.0 for item in source],
    }


def _rerun_case(config: dict, rounds_per_check: int) -> dict:
    simulation = run_no_ui_simulation(config, rounds_per_check)
    scored = score_stability(simulation)

    state = simulation["state"]
    node_charge_map = {
        int(node.id): float(node.q_total)
        for node in state["nodes"].values()
    }

    edge_weight_map: dict[str, float] = {}
    for edge in state.get("edges", []) or []:
        a = int(edge["a"])
        b = int(edge["b"])
        key = _edge_key(a, b)
        stat = (state.get("linkStats") or {}).get(key, {})
        edge_weight_map[key] = float(stat.get("effectiveQuality", edge.get("quality", 0.0)))

    total_q = sum(node_charge_map.values())
    avg_q = total_q / max(1, len(node_charge_map))
    total_w = sum(edge_weight_map.values())
    avg_w = total_w / max(1, len(edge_weight_map))

    return {
        "score": float(scored.get("score", 0.0)),
        "verdict": str(scored.get("verdict", "UNSTABLE")),
        "scoreMetrics": scored.get("metrics", {}) or {},
        "scoreRationale": scored.get("rationale", []) or [],
        "snapshots": simulation.get("snapshots", []) or [],
        "chartSeries": _build_chart_series_from_snapshots(simulation.get("snapshots", []) or []),
        "topology": _compact_topology_from_state(state),
        "nodeChargeMap": node_charge_map,
        "edgeWeightMap": edge_weight_map,
        "summaryLines": [
            f"Total q: {total_q:.1f}",
            f"Mean q: {avg_q:.1f}",
            f"Total edge weight: {total_w:.2f}",
            f"Mean edge weight: {avg_w:.2f}",
        ],
    }


def _pick_worst_runs(evaluation_runs: list[dict]) -> dict[str, dict | None]:
    if not evaluation_runs:
        return {"globalWorst": None, "unstableWorst": None}

    global_worst = min(evaluation_runs, key=lambda item: float(item.get("score", 0.0)))

    unstable = [item for item in evaluation_runs if str(item.get("verdict", "")) == "UNSTABLE"]
    oscillating = [item for item in evaluation_runs if str(item.get("verdict", "")) == "OSCILLATING"]

    unstable_worst = None
    if unstable:
        unstable_worst = min(unstable, key=lambda item: float(item.get("score", 0.0)))
    elif oscillating:
        unstable_worst = min(oscillating, key=lambda item: float(item.get("score", 0.0)))

    return {"globalWorst": global_worst, "unstableWorst": unstable_worst}


def _value_or_na(value) -> str:
    if value is None:
        return "n/a"
    return str(int(value))


def _check_label(key: str) -> str:
    labels = {
        "assumptionsPass": "Assumptions",
        "theoremPass": "Theorem",
        "a5": "A5",
        "a6": "A6",
        "a7": "A7",
        "lemma41": "Lemma41",
        "lemma42": "Lemma42",
        "lemma43": "Lemma43",
    }
    return labels.get(key, key)


def _activation_rows(network_details: list[dict]) -> list[dict]:
    rows: list[dict] = []
    for item in network_details:
        activation = item.get("theoremActivation", {})
        rows.append(
            {
                "networkId": item.get("id", "n/a"),
                "label": item.get("label", item.get("id", "n/a")),
                "firstAssumptionsPassRound": activation.get("firstAssumptionsPassRound"),
                "firstTheoremPassRound": activation.get("firstTheoremPassRound"),
                "firstAllChecksPassRound": activation.get("firstAllChecksPassRound"),
                "sustainedAssumptionsPassFromRound": activation.get(
                    "sustainedAssumptionsPassFromRound"
                ),
                "sustainedTheoremPassFromRound": activation.get(
                    "sustainedTheoremPassFromRound"
                ),
                "sustainedAllChecksPassFromRound": activation.get(
                    "sustainedAllChecksPassFromRound"
                ),
            }
        )
    return rows


def _build_table(headers: list[str], rows: list[list[str]]) -> list[str]:
    lines: list[str] = []
    lines.append("| " + " | ".join(headers) + " |")
    lines.append("| " + " | ".join(["---"] * len(headers)) + " |")
    for row in rows:
        lines.append("| " + " | ".join(row) + " |")
    return lines


def _write_summary_md(
    path: Path,
    *,
    request: dict,
    result: dict,
    report_data: dict,
    started_at: datetime,
    finished_at: datetime,
    chart_files: list[Path],
    activation_rows: list[dict],
) -> None:
    metadata = result.get("metadata", {})
    recommendations = report_data.get("recommendations", [])
    best = _pick_best_network(report_data.get("networkDetails", []))

    duration_seconds = max(0.0, (finished_at - started_at).total_seconds())

    lines: list[str] = []
    lines.append("# Batch Run Summary")
    lines.append("")
    lines.append("## Run")
    lines.append("")
    lines.append(f"- Started: {started_at.isoformat()}")
    lines.append(f"- Finished: {finished_at.isoformat()}")
    lines.append(f"- DurationSec: {duration_seconds:.2f}")
    lines.append(f"- Topologies: {int(metadata.get('topologyCount', 0))}")
    lines.append(f"- TotalRuns: {int(metadata.get('totalRuns', 0))}")
    lines.append(f"- OptimizationIterations: {int(metadata.get('optimizationIterations', 0))}")
    lines.append(f"- SeedStart: {int(metadata.get('seedStart', 0))}")
    lines.append(f"- SeedCount: {int(metadata.get('seedCount', 0))}")
    lines.append(f"- RoundsPerCheck: {int(metadata.get('roundsPerCheck', 0))}")
    lines.append(f"- ParallelWorkers: {int(metadata.get('parallelWorkers', 0))}")
    lines.append("")

    lines.append("## Best Recommendation")
    lines.append("")
    if recommendations:
        best_rec = max(recommendations, key=lambda item: float(item.get("avgScore", 0)))
        lines.append(f"- Network: {best_rec.get('label', best_rec.get('networkId', 'n/a'))}")
        lines.append(f"- Verdict: {best_rec.get('verdict', 'n/a')}")
        lines.append(f"- AvgScore: {float(best_rec.get('avgScore', 0)):.2f}")
        lines.append(f"- StableRatio: {float(best_rec.get('stableRatio', 0)):.3f}")
        lines.append(f"- BestSeed: {best_rec.get('bestSeed', 'n/a')}")
    else:
        lines.append("- No recommendations available")
    lines.append("")

    lines.append("## Axiom and Theorem Activation by Network")
    lines.append("")
    if activation_rows:
        table_rows: list[list[str]] = []
        for row in activation_rows:
            table_rows.append(
                [
                    str(row.get("networkId", "n/a")),
                    _value_or_na(row.get("firstAssumptionsPassRound")),
                    _value_or_na(row.get("firstTheoremPassRound")),
                    _value_or_na(row.get("firstAllChecksPassRound")),
                    _value_or_na(row.get("sustainedAssumptionsPassFromRound")),
                    _value_or_na(row.get("sustainedTheoremPassFromRound")),
                    _value_or_na(row.get("sustainedAllChecksPassFromRound")),
                ]
            )

        lines.extend(
            _build_table(
                [
                    "Network",
                    "FirstAssumptions",
                    "FirstTheorem",
                    "FirstAllChecks",
                    "SustainedAssumptions",
                    "SustainedTheorem",
                    "SustainedAllChecks",
                ],
                table_rows,
            )
        )
    else:
        lines.append("No activation statistics available.")
    lines.append("")

    lines.append("## Best Network Check Activation Detail")
    lines.append("")
    if best:
        activation = best.get("theoremActivation", {})
        pass_rate = activation.get("passRate", {})
        first_by_check = activation.get("firstByCheckRound", {})
        sustained_by_check = activation.get("sustainedByCheckRound", {})

        detail_rows: list[list[str]] = []
        for key in CHECK_ORDER:
            detail_rows.append(
                [
                    _check_label(key),
                    _value_or_na(first_by_check.get(key)),
                    _value_or_na(sustained_by_check.get(key)),
                    f"{float(pass_rate.get(key, 0)) * 100:.2f}",
                ]
            )

        lines.extend(
            _build_table(
                ["Check", "FirstPassRound", "SustainedFromRound", "PassRatePercent"],
                detail_rows,
            )
        )
    else:
        lines.append("No best network detail available.")
    lines.append("")

    lines.append("## Input Request")
    lines.append("")
    lines.append("```json")
    lines.append(json.dumps(request, ensure_ascii=False, indent=2))
    lines.append("```")
    lines.append("")

    lines.append("## Charts")
    lines.append("")
    if chart_files:
        for chart in chart_files:
            rel = chart.as_posix()
            lines.append(f"### {chart.stem}")
            lines.append("")
            lines.append(f"![{chart.stem}]({rel})")
            lines.append("")
    else:
        lines.append("No charts were generated.")
        lines.append("")

    lines.append("## Notes")
    lines.append("")
    if best and best.get("scoreRationale"):
        for reason in best["scoreRationale"]:
            lines.append(f"- {reason}")
    else:
        lines.append("- No additional score rationale for best run.")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def save_run_artifacts(
    *,
    request: dict,
    result: dict,
    res_root: Path,
    started_at: datetime,
    finished_at: datetime,
) -> dict:
    run_dir = _ensure_unique_run_dir(res_root, started_at)
    charts_dir = run_dir / "charts"
    charts_dir.mkdir(parents=True, exist_ok=True)

    full_report_path = run_dir / "batch_report.json"
    full_report_path.write_text(
        json.dumps(result, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    report_data = build_report_data(result)
    report_data_path = run_dir / "report_data.json"
    report_data_path.write_text(
        json.dumps(report_data, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    request_path = run_dir / "request.json"
    request_path.write_text(
        json.dumps(request, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    matrix = report_data.get("matrix", [])
    recommendations = report_data.get("recommendations", [])
    network_details = report_data.get("networkDetails", [])
    evaluation_runs = result.get("evaluationRuns", []) or []
    best_network = _pick_best_network(network_details)
    activation_rows = _activation_rows(network_details)
    worst_runs = _pick_worst_runs(evaluation_runs)

    base_config = (request.get("baseConfig") or {}).copy()
    rounds_per_check = int(
        (result.get("metadata") or {}).get(
            "roundsPerCheck",
            request.get("roundsPerCheck", base_config.get("maxRounds", 280)),
        )
        or 280
    )

    worst_score_by_network: dict[str, float] = {}
    unstable_or_oscillating_by_network: dict[str, float] = {}
    for row in evaluation_runs:
        network_id = str(row.get("networkId", "n/a"))
        score = float(row.get("score", 0.0))
        verdict = str(row.get("verdict", "UNSTABLE"))

        if network_id not in worst_score_by_network:
            worst_score_by_network[network_id] = score
        else:
            worst_score_by_network[network_id] = min(worst_score_by_network[network_id], score)

        if verdict != "STABLE":
            unstable_or_oscillating_by_network[network_id] = (
                unstable_or_oscillating_by_network.get(network_id, 0.0) + 1.0
            )

    network_order = [str(item.get("networkId", "n/a")) for item in matrix]
    worst_score_entries = [
        (network_id, float(worst_score_by_network.get(network_id, 0.0)))
        for network_id in network_order
    ]
    unstable_entries = [
        (network_id, float(unstable_or_oscillating_by_network.get(network_id, 0.0)))
        for network_id in network_order
    ]

    best_case = None
    if best_network:
        best_seed = int(best_network.get("bestRunSeed") or base_config.get("seed", 42) or 42)
        best_case_config = {
            **base_config,
            **(best_network.get("bestParameters") or {}),
            "nodeCount": int(best_network.get("nodeCount", base_config.get("nodeCount", 34))),
            "linkRadius": int(best_network.get("linkRadius", base_config.get("linkRadius", 195))),
            "seed": best_seed,
            "maxRounds": rounds_per_check,
        }
        best_case = _rerun_case(best_case_config, rounds_per_check)

    worst_case = None
    worst_case_row = worst_runs.get("unstableWorst") or worst_runs.get("globalWorst")
    if worst_case_row:
        worst_seed = int(worst_case_row.get("seed", base_config.get("seed", 42)) or 42)
        worst_case_config = {
            **base_config,
            **(worst_case_row.get("params") or {}),
            "nodeCount": int(worst_case_row.get("nodeCount", base_config.get("nodeCount", 34))),
            "linkRadius": int(worst_case_row.get("linkRadius", base_config.get("linkRadius", 195))),
            "seed": worst_seed,
            "maxRounds": rounds_per_check,
        }
        worst_case = _rerun_case(worst_case_config, rounds_per_check)

    score_entries = [
        (str(item.get("networkId", "n/a")), float(item.get("avgScore", 0)))
        for item in matrix
    ]
    stable_ratio_entries = [
        (str(item.get("networkId", "n/a")), float(item.get("stableRatio", 0)) * 100)
        for item in matrix
    ]
    verdict_entries = _build_verdict_counts(recommendations)

    chart_paths: list[Path] = []

    score_chart = charts_dir / "score_by_network.svg"
    write_bar_chart(
        score_chart,
        title="Average Score by Network",
        entries=score_entries,
        y_label="Score",
        y_max=100.0,
        color="#3572c9",
    )
    chart_paths.append(score_chart)

    ratio_chart = charts_dir / "stable_ratio_by_network.svg"
    write_bar_chart(
        ratio_chart,
        title="Stable Ratio by Network",
        entries=stable_ratio_entries,
        y_label="StableRatioPercent",
        y_max=100.0,
        color="#2ca56b",
    )
    chart_paths.append(ratio_chart)

    verdict_chart = charts_dir / "verdict_distribution.svg"
    write_bar_chart(
        verdict_chart,
        title="Verdict Distribution",
        entries=verdict_entries,
        y_label="Count",
        y_max=None,
        color="#d37f2d",
    )
    chart_paths.append(verdict_chart)

    worst_score_chart = charts_dir / "worst_score_by_network.svg"
    write_bar_chart(
        worst_score_chart,
        title="Worst Score by Network (all evaluated runs)",
        entries=worst_score_entries,
        y_label="Score",
        y_max=100.0,
        color="#c65b4c",
    )
    chart_paths.append(worst_score_chart)

    unstable_runs_chart = charts_dir / "unstable_or_oscillating_runs_by_network.svg"
    write_bar_chart(
        unstable_runs_chart,
        title="Unstable/Oscillating Run Count by Network",
        entries=unstable_entries,
        y_label="RunCount",
        y_max=None,
        color="#9152ba",
    )
    chart_paths.append(unstable_runs_chart)

    first_all_checks_chart = charts_dir / "first_all_checks_round_by_network.svg"
    write_bar_chart(
        first_all_checks_chart,
        title="First Round Where All Axioms and Theorems Pass",
        entries=[
            (
                str(item.get("networkId", "n/a")),
                float(item.get("firstAllChecksPassRound") or 0),
            )
            for item in activation_rows
        ],
        y_label="Round",
        y_max=None,
        color="#8a62d4",
    )
    chart_paths.append(first_all_checks_chart)

    sustained_all_checks_chart = charts_dir / "sustained_all_checks_round_by_network.svg"
    write_bar_chart(
        sustained_all_checks_chart,
        title="Round From Which All Axioms and Theorems Stay Passing",
        entries=[
            (
                str(item.get("networkId", "n/a")),
                float(item.get("sustainedAllChecksPassFromRound") or 0),
            )
            for item in activation_rows
        ],
        y_label="Round",
        y_max=None,
        color="#4ea98f",
    )
    chart_paths.append(sustained_all_checks_chart)

    if best_network:
        series = (best_case or {}).get("chartSeries") or best_network.get("chartSeries", {})
        activation = best_network.get("theoremActivation", {})

        topology_chart = charts_dir / "best_network_topology.svg"
        write_network_topology_chart(
            topology_chart,
            title=(
                f"Best Network Topology ({best_network.get('id', 'n/a')}) "
                f"score={float((best_case or {}).get('score', best_network.get('bestAvgScore', 0.0))):.2f}"
            ),
            topology=(best_case or {}).get("topology") or best_network.get("topology"),
            node_charge_map=(best_case or {}).get("nodeChargeMap"),
            edge_weight_map=(best_case or {}).get("edgeWeightMap"),
            summary_lines=(best_case or {}).get("summaryLines", []),
        )
        chart_paths.append(topology_chart)

        coverage_chart = charts_dir / "best_network_coverage.svg"
        write_line_chart(
            coverage_chart,
            title=f"Coverage Dynamics ({best_network.get('id', 'n/a')})",
            series={"coveragePercent": series.get("coveragePercent", [])},
            y_label="CoveragePercent",
        )
        chart_paths.append(coverage_chart)

        stability_chart = charts_dir / "best_network_stability.svg"
        write_line_chart(
            stability_chart,
            title=f"Parent Changes and Flapping ({best_network.get('id', 'n/a')})",
            series={
                "parentChanges": series.get("parentChanges", []),
                "flappingNodes": series.get("flappingNodes", []),
            },
            y_label="Count",
        )
        chart_paths.append(stability_chart)

        connectivity_chart = charts_dir / "best_network_connectivity_dynamics.svg"
        write_line_chart(
            connectivity_chart,
            title=f"Connectivity and Eligibility Dynamics ({best_network.get('id', 'n/a')})",
            series={
                "coveragePercent": series.get("coveragePercent", []),
                "eligibleCount": series.get("eligibleCount", []),
            },
            y_label="Value",
        )
        chart_paths.append(connectivity_chart)

        routing_chart = charts_dir / "best_network_routing_dynamics.svg"
        write_line_chart(
            routing_chart,
            title=f"Routing and Traffic Dynamics ({best_network.get('id', 'n/a')})",
            series={
                "duplicates": series.get("duplicates", []),
                "upHops": series.get("upHops", []),
                "propagationDeliveries": series.get("propagationDeliveries", []),
                "spreadUpdates": series.get("spreadUpdates", []),
            },
            y_label="Count",
        )
        chart_paths.append(routing_chart)

        theorem_status_chart = charts_dir / "best_network_theorem_status.svg"
        write_line_chart(
            theorem_status_chart,
            title=f"Theorem and Assumption Pass Status ({best_network.get('id', 'n/a')})",
            series={
                "assumptionsPass": series.get("assumptionsPassBinary", []),
                "theoremPass": series.get("theoremPassBinary", []),
                "allChecks": series.get("allChecksBinary", []),
            },
            y_label="Pass(0or1)",
        )
        chart_paths.append(theorem_status_chart)

        checks_status_chart = charts_dir / "best_network_checks_status.svg"
        write_line_chart(
            checks_status_chart,
            title=f"Axiom and Lemma Status by Round ({best_network.get('id', 'n/a')})",
            series={
                "A5": series.get("a5Binary", []),
                "A6": series.get("a6Binary", []),
                "A7": series.get("a7Binary", []),
                "L41": series.get("lemma41Binary", []),
                "L42": series.get("lemma42Binary", []),
                "L43": series.get("lemma43Binary", []),
            },
            y_label="Pass(0or1)",
        )
        chart_paths.append(checks_status_chart)

        first_by_check = activation.get("firstByCheckRound", {})
        first_by_check_chart = charts_dir / "best_network_first_pass_round_by_check.svg"
        write_bar_chart(
            first_by_check_chart,
            title=f"First Pass Round by Check ({best_network.get('id', 'n/a')})",
            entries=[
                (_check_label(key), float(first_by_check.get(key) or 0))
                for key in CHECK_ORDER
            ],
            y_label="Round",
            y_max=None,
            color="#ce5f5f",
        )
        chart_paths.append(first_by_check_chart)

        sustained_by_check = activation.get("sustainedByCheckRound", {})
        sustained_by_check_chart = charts_dir / "best_network_sustained_pass_round_by_check.svg"
        write_bar_chart(
            sustained_by_check_chart,
            title=f"Sustained Pass Round by Check ({best_network.get('id', 'n/a')})",
            entries=[
                (_check_label(key), float(sustained_by_check.get(key) or 0))
                for key in CHECK_ORDER
            ],
            y_label="Round",
            y_max=None,
            color="#d08438",
        )
        chart_paths.append(sustained_by_check_chart)

    if worst_case and worst_case_row:
        worst_label = str(worst_case_row.get("networkId", "n/a"))
        worst_verdict = str(worst_case.get("verdict", worst_case_row.get("verdict", "UNSTABLE")))
        worst_seed = int(worst_case_row.get("seed", 0) or 0)
        worst_series = worst_case.get("chartSeries", {})

        worst_topology_chart = charts_dir / "worst_network_topology.svg"
        write_network_topology_chart(
            worst_topology_chart,
            title=f"Worst Network Topology ({worst_label}) score={float(worst_case.get('score', 0.0)):.2f}",
            topology=worst_case.get("topology"),
            node_charge_map=worst_case.get("nodeChargeMap"),
            edge_weight_map=worst_case.get("edgeWeightMap"),
            summary_lines=[
                *worst_case.get("summaryLines", []),
                f"Verdict: {worst_verdict}",
                f"Seed: {worst_seed}",
            ],
        )
        chart_paths.append(worst_topology_chart)

        worst_connectivity_chart = charts_dir / "worst_network_connectivity_dynamics.svg"
        write_line_chart(
            worst_connectivity_chart,
            title=f"Worst Case Connectivity/Eligible ({worst_label})",
            series={
                "coveragePercent": worst_series.get("coveragePercent", []),
                "eligibleCount": worst_series.get("eligibleCount", []),
            },
            y_label="Value",
        )
        chart_paths.append(worst_connectivity_chart)

        worst_stability_chart = charts_dir / "worst_network_stability.svg"
        write_line_chart(
            worst_stability_chart,
            title=f"Worst Case Parent Changes/Flapping ({worst_label})",
            series={
                "parentChanges": worst_series.get("parentChanges", []),
                "flappingNodes": worst_series.get("flappingNodes", []),
            },
            y_label="Count",
        )
        chart_paths.append(worst_stability_chart)

        worst_routing_chart = charts_dir / "worst_network_routing_dynamics.svg"
        write_line_chart(
            worst_routing_chart,
            title=f"Worst Case Routing/Traffic ({worst_label})",
            series={
                "duplicates": worst_series.get("duplicates", []),
                "upHops": worst_series.get("upHops", []),
                "propagationDeliveries": worst_series.get("propagationDeliveries", []),
                "spreadUpdates": worst_series.get("spreadUpdates", []),
            },
            y_label="Count",
        )
        chart_paths.append(worst_routing_chart)

        worst_theorem_chart = charts_dir / "worst_network_theorem_status.svg"
        write_line_chart(
            worst_theorem_chart,
            title=f"Worst Case Theorem/Assumptions Status ({worst_label})",
            series={
                "assumptionsPass": worst_series.get("assumptionsPassBinary", []),
                "theoremPass": worst_series.get("theoremPassBinary", []),
                "allChecks": worst_series.get("allChecksBinary", []),
            },
            y_label="Pass(0or1)",
        )
        chart_paths.append(worst_theorem_chart)

        worst_checks_chart = charts_dir / "worst_network_checks_status.svg"
        write_line_chart(
            worst_checks_chart,
            title=f"Worst Case Axiom/Lemma Status ({worst_label})",
            series={
                "A5": worst_series.get("a5Binary", []),
                "A6": worst_series.get("a6Binary", []),
                "A7": worst_series.get("a7Binary", []),
                "L41": worst_series.get("lemma41Binary", []),
                "L42": worst_series.get("lemma42Binary", []),
                "L43": worst_series.get("lemma43Binary", []),
            },
            y_label="Pass(0or1)",
        )
        chart_paths.append(worst_checks_chart)

    summary_md_path = run_dir / "run_summary.md"
    _write_summary_md(
        summary_md_path,
        request=request,
        result=result,
        report_data=report_data,
        started_at=started_at,
        finished_at=finished_at,
        chart_files=[path.relative_to(run_dir) for path in chart_paths],
        activation_rows=activation_rows,
    )

    return {
        "runDir": run_dir,
        "fullReport": full_report_path,
        "reportData": report_data_path,
        "request": request_path,
        "summaryMd": summary_md_path,
        "charts": chart_paths,
    }
