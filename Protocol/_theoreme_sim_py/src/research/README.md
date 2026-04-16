# research

Batch research mode for topology/parameter/seed sweeps.

Files:
- network_matrix_parser.py: matrix parsing into topology combinations
- parameter_search_space.py: vector encode/decode and random direction generation
- stability_scorer.py: score and verdict from simulation snapshots
- batch_research_runner.py: adaptive optimization loop across topologies
- report_data_builder.py: compact chart/report payload builder
- comprehensive_study.py: cross-topology sensitivity, correlation, and full report artifacts
- svg_charts.py: dependency-free SVG chart generation
- run_artifacts.py: timestamped run-directory save pipeline (json + charts + markdown)

Generated artifact charts include:
- per-network score/stability/verdict distributions
- first and sustained rounds for full theorem+axiom pass
- best-network topology graph (nodes + edges)
- detailed best-network round dynamics and pass-status timelines
