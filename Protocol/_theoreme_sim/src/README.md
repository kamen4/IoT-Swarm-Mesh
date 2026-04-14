# src

This folder contains the simulation source code split into small domain modules.

## Subfolders

- core/ - Fundamental constants, graph helpers, and data types.
- config/ - Parameter range metadata and full random config generation.
- generation/ - Topology and initial charge generation.
- phases/ - Per-round DOWN and UP phase simulation helpers.
- propagation/ - Neighbor charge learning and decay operations.
- routing/ - Parent selection and tree rebuilding logic.
- verification/ - Assumption checks and theorem checks.
- broadcast/ - DOWN tree-broadcast simulation logic.
- export/ - No-UI snapshot shaping and JSON/CSV export helpers.
- research/ - Batch topology/parameter research runner, compact pass metrics, and HTML reporting.
- render/ - Canvas graph rendering, colors, and edge styles.
- ui/ - Control panel and metrics panel rendering.
- state/ - Simulation state factory and execution controller.

## Files

- main.js - App bootstrap and module wiring.
