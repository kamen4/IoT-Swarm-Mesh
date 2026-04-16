# Validation Matrix

## Matrix Fields

- invariant
- check-method
- simulation-evidence
- hardware-evidence
- owner-task

## Wave 1 Matrix

| Invariant | Check Method | Simulation Evidence | Hardware Evidence | Owner Task |
| --- | --- | --- | --- | --- |
| Envelope conformance | parse/encode roundtrip tests | frame tests in sim harness | two-node frame exchange logs | W1-FND-003 |
| Lifecycle order | state transition tests | onboarding flow replay | onboarding run logs | W1-PRO-003 |
| UP dedup + ttl guard | route stress tests | repeated packet suppression curves | multi-hop duplicate drop logs | W1-PRO-104 |
| DOWN tree behavior | parent-child consistency checks | theorem-linked tree checks | child forwarding traces | W1-PRO-203 |
| Charge decay stability | long-run convergence checks | decay epoch reports | stability logs by epoch | W1-PRO-206 |
| Baseline lock | config snapshot compare | baseline parity report | runtime baseline dump | W1-PAR-005 |

## Gate Rule

All rows must have evidence before readiness checklist can be marked pass.

## Evidence Tracking and Closure

1. Each owner task MUST attach at least one simulation evidence artifact and one hardware evidence artifact.
2. Evidence links MUST be recorded in this file during task closure.
3. Evidence review result MUST be recorded in Implementation/DevPlan/92-audit-log.md.
4. Evidence artifact paths and naming MUST follow Implementation/DevPlan/evidence-artifact-convention.md.

## Source Pointers

- Protocol/_docs_v1.0/mitigations/simulation-pipeline.md
- Protocol/_docs_v1.0/math/theorem.md
- Protocol/_theoreme_ai_conclusion/report.md
- Implementation/DevPlan/evidence-artifact-convention.md
