# Validation Strategy

## Objective

Define quality gates for Wave 1 implementation readiness.

## Validation Layers

- Protocol conformance checks (envelope, message types, lifecycle transitions)
- Routing behavior checks (UP/DOWN contracts and dedup/ttl guards)
- Stability checks (charge convergence and decay behavior)
- Theorem-linked checks (operational observability of assumptions)

## Required Evidence

- Deterministic simulation evidence using canonical phase order.
- Baseline parity evidence for cand2_more_inertia defaults.
- Integration evidence for onboarding and delivery flows.

## Readiness Gates

- Traceability gate: each requirement references sources.
- Terminology gate: preserved names and symbols.
- Baseline gate: all 13 defaults match fixed baseline.
- Scope gate: Wave 1 boundaries respected.

## Source Pointers

- Protocol/_docs_v1.0/mitigations/simulation-pipeline.md
- Protocol/_docs_v1.0/math/theorem.md
- Protocol/_theoreme_ai_conclusion/report.md
