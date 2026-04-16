# Wave 1 Integration: Dependency Map

## Purpose

Track blocking and parallel execution relationships across Wave 1 tasks.

## Blocking Chains

- W1-FND-001 -> W1-FND-002 -> W1-FND-003
- W1-FND-002 -> W1-FND-009 -> W1-FND-010 -> W1-FND-011 -> W1-FND-012 -> W1-FND-013
- W1-FND-008 -> W1-FND-014 -> W1-FND-018
- W1-PRO-001 -> W1-PRO-002 -> W1-PRO-003 -> W1-PRO-004 -> W1-PRO-005
- W1-PRO-003 -> W1-PRO-009 -> W1-PRO-010 -> W1-PRO-011 -> W1-PRO-012 -> W1-PRO-013 -> W1-PRO-015
- W1-FND-006 -> W1-PRO-201 -> W1-PRO-202 -> W1-PRO-203 -> W1-PRO-205
- W1-PRO-201 -> W1-PRO-204 -> W1-PRO-206
- W1-PRO-203 -> W1-PRO-207
- W1-PRO-101 -> W1-PRO-108 -> W1-PRO-110 -> W1-PRO-114
- W1-PRO-105 -> W1-PRO-111 -> W1-PRO-112 -> W1-PRO-115
- W1-PAR-002 -> W1-PAR-008 -> W1-PAR-010
- W1-PAR-005 -> W1-PAR-012 -> W1-PAR-013
- W1-INT-101 -> W1-INT-102/103/104 -> W1-INT-107 -> W1-INT-108
- W1-INT-201 -> W1-INT-206 -> W1-INT-208 -> W1-INT-209
- W1-INT-202 -> W1-INT-210
- W1-INT-205 -> W1-INT-211

## Parallel Streams

- Foundation gateway stream (W1-FND-101..106) can run alongside firmware stream after boundary definition.
- Baseline parameter stream (W1-PAR-001..006) can progress once spec core is stable.
- Observability stream (W1-INT-201..205) can run in parallel after diagnostics hooks exist.

## Gate Dependencies

- Validation matrix rows require owner tasks to complete and attach evidence.
- Risk register verification signals require mitigation task completion.
- Readiness checklist cannot pass until traceability, baseline, recheck, and consistency fields are PASS.

## Source Pointers

- Implementation/DevPlan/90-validation-matrix.md
- Implementation/DevPlan/91-risk-register.md
- Implementation/DevPlan/99-readiness-checklist.md
- Implementation/Spec/00-source-map.md
