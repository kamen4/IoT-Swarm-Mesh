# research

Batch research mode for long-run stability studies across topology and parameter grids.

## Goal

Automate repeated no-UI simulations to answer:

- which parameter sets are stable for each network shape,
- how metrics evolve over long horizons,
- which parameter dependencies correlate with stability.

## Pipeline

1. Parse topology matrix and seed settings from UI.
2. Start from base parameter vector and evaluate stability score.
3. Run adaptive gradient-like search with plateau escape: sparse-direction probing, objective with stability-health signals, dynamic step reheat on stagnation, and forced exploration when hold dominates.
4. Keep best parameter vector per topology and re-evaluate it with full artifacts.
5. Build compact PASS and score diagnostics from best-run snapshots.
6. Build a self-contained HTML report with topology canvas, best parameter vector, and per-topology charts.
7. Export HTML only; report page contains buttons to download JSON and CSV from embedded data.

## Files

- networkMatrixParser.js - Parse network matrix text into {nodeCount, linkRadius} grid.
- parameterSearchSpace.js - Adaptive optimization vector encoding/decoding and tunable parameter metadata.
- stabilityScorer.js - Compute comparable stability score from run snapshots.
- batchResearchRunner.js - Execute adaptive optimization per topology and aggregate best runs.
- reportDataBuilder.js - Convert raw run output into chart-friendly report data.
- htmlReportBuilder.js - Build standalone HTML report with embedded charts/data.
- researchExporter.js - Save HTML report output.
- metrics/passMetrics.js - Build compact theorem/assumption pass counters and rates.
- optimization/candidateSelection.js - Candidate preference and adaptive step-size heuristics.
