# Agent Launch Checklist

Use this checklist before starting the autonomous full-product agent run.

## Inputs and policy

- [ ] Confirm instruction files are loaded from Implementation/.github/instructions.
- [ ] Confirm full-product spec exists and is the active target.
- [ ] Confirm master plan file is selected for execution ordering.
- [ ] Confirm baseline parameter source file path is valid.

## Required source files

- [ ] Implementation/Spec/00-source-map.md
- [ ] Implementation/Spec/06-full-product/01-full-product-spec.md
- [ ] Implementation/DevPlan/95-full-product-master-plan.md
- [ ] Implementation/DevPlan/90-validation-matrix.md
- [ ] Implementation/DevPlan/91-risk-register.md
- [ ] Implementation/DevPlan/92-audit-log.md
- [ ] Implementation/DevPlan/bootstrap-timeout-retry-decision.md
- [ ] Implementation/DevPlan/evidence-artifact-convention.md
- [ ] Implementation/DevPlan/99-readiness-checklist.md

## Runtime discipline

- [ ] Execute only unblocked tasks based on dependencies.
- [ ] Work in batches and update quality artifacts after each batch.
- [ ] Run subagent consistency check after each non-trivial change.
- [ ] Record blockers and provisional decisions immediately.
- [ ] Stop and escalate if blocker termination criteria are reached.

## Completion criteria

- [ ] Validation evidence attached for critical invariants.
- [ ] Risk mitigations linked to completed tasks.
- [ ] Readiness gates updated with evidence-backed status.
- [ ] Final handoff report generated.

## Source Pointers

- Implementation/DevPlan/94-execution-protocol.md
- Implementation/DevPlan/95-full-product-master-plan.md
- Implementation/Spec/06-full-product/01-full-product-spec.md
