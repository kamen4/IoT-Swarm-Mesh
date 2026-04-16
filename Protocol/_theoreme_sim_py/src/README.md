# src

Python simulation source split into small focused modules.

Subfolders:
- core: constants, RNG, graph helpers, contracts
- config: parameter ranges and normalization
- generation: topology and initial charge setup
- propagation: per-round charge/link dynamics
- phases: DOWN, UP, DECAY phase logic
- routing: parent selection and tree rebuilding
- verification: A5/A6/A7 and theorem lemmas checks
- export: round/final snapshot builders
- state: state creation and shared round pipeline
- research: batch optimization and reporting data builders
- utils: shared numeric helpers
