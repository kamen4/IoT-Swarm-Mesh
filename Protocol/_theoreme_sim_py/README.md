# _theoreme_sim_py

Python port of batch-focused theorem simulation from ../_theoreme_sim.

Goal:
- keep theorem semantics and verification checks identical (A5/A6/A7, Lemma 4.1/4.2/4.3)
- speed up large batch studies by using multi-process seed evaluation
- keep small, auditable modules and nested folder documentation

## Scope

This port focuses on headless simulation and research batch runs.
UI rendering and browser-specific modules are intentionally not included.

## Structure

- src/core: constants, seeded RNG, graph helpers, data contracts
- src/config: parameter ranges and config normalization
- src/generation: topology generation and charge initialization
- src/propagation: neighbor learning, charge spread, decay, link tracking
- src/phases: DOWN, UP, and decay phases
- src/routing: parent selection and tree rebuild
- src/verification: assumptions/theorem checks and oscillation metrics
- src/export: round/final snapshot builders
- src/state: simulation state and round pipeline
- src/research: batch runner, parameter search, scoring, report data

## Quick Start

From _theoreme_sim_py:

1. Run a minimal batch study:
   python run_batch.py --quick

2. Run with request JSON:
   python run_batch.py --request request.json --output report.json

3. Run comprehensive sensitivity study profile:
   python run_batch.py --comprehensive --res-root ../_theoreme_ai_search/try_1

4. Default artifact layout:
   - res/YYYY-MM-DD_HH-MM-SS/batch_report.json
   - res/YYYY-MM-DD_HH-MM-SS/report_data.json
   - res/YYYY-MM-DD_HH-MM-SS/request.json
   - res/YYYY-MM-DD_HH-MM-SS/run_summary.md
   - res/YYYY-MM-DD_HH-MM-SS/charts/*.svg
   - res/YYYY-MM-DD_HH-MM-SS/comprehensive_report.md (for --comprehensive)
   - res/YYYY-MM-DD_HH-MM-SS/comprehensive_analysis.json (for --comprehensive)

## Notes on Speed

Batch acceleration is implemented in src/research/batch_research_runner.py:
- seed-level evaluation can run in parallel with ProcessPoolExecutor
- fallback to deterministic sequential execution is available
- no UI overhead and compact snapshot pipelines

## Run Artifacts

Each run stores a timestamped folder under res by default.

The markdown summary includes:
- start/end time and duration
- topology/run counts and optimization settings
- best recommendation overview
- embedded charts with run statistics
