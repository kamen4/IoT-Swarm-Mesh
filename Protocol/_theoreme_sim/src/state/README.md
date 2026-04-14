# state

Simulation state creation and round execution controller.

## Files

- simulationState.js - Build initial graph, charges, and estimate tables.
- simulationController.js - Run rounds, evaluate theorem, trigger rendering, and orchestrate research batch exports.
- simulationStep.js - Shared per-round update pipeline for UI and headless runs.
- noUiSimulationRunner.js - Execute max-step headless simulation and collect snapshots.

## Round pipeline

- Phase 1: DOWN round (root-origin packet, duplicate tracking, coverage).
- Phase 2: UP round (device attempts to route toward gateway).
- Phase 3: Neighbor propagation + spread smoothing.
- Phase 4: Link-strength finalization and optional DECAY epoch.
- Phase 5: Tree rebuild, oscillation metrics, and theorem checks.
