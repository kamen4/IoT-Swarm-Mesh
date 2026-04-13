# src

This folder contains the simulation source code split into small domain modules.

## Subfolders

- core/ - Fundamental constants, graph helpers, and data types.
- generation/ - Topology and initial charge generation.
- propagation/ - Neighbor charge learning and decay operations.
- routing/ - Parent selection and tree rebuilding logic.
- verification/ - Assumption checks and theorem checks.
- broadcast/ - DOWN tree-broadcast simulation logic.
- render/ - Canvas graph rendering, colors, and edge styles.
- ui/ - Control panel and metrics panel rendering.
- state/ - Simulation state factory and execution controller.

## Files

- main.js - App bootstrap and module wiring.
