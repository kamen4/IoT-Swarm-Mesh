---
description: Load these rules when executing Wave1 delivery from DevPlan tasks, including implementation sequencing, evidence capture, and gate progression.
applyTo: "Implementation/DevPlan/**"
name: "Wave1 Execution Protocol"
---

# Wave1 execution instructions

## Execution order

1. Select only unblocked tasks.
2. Prefer shortest valid dependency chain first.
3. Execute in small batches with measurable outputs.
4. Attach evidence immediately after each batch.

## Task selection rules

- Never start a task with unresolved dependencies.
- Prefer tasks that unblock multiple downstream tasks.
- Keep firmware, gateway, hub, protocol, and integration streams balanced.

## Completion package per task

For each completed task, record:

- implemented artifact reference
- test or verification evidence reference
- metric or log evidence when relevant
- follow-up items if partially complete
- evidence path and naming compliant with Implementation/DevPlan/evidence-artifact-convention.md

## Required post-batch updates

After each non-trivial batch, update:

- Implementation/DevPlan/90-validation-matrix.md
- Implementation/DevPlan/92-audit-log.md
- Implementation/DevPlan/99-readiness-checklist.md

## Blocker handling

If a task is blocked by missing normative decision:

- create PROVISIONAL decision record in audit log
- link blocker to the exact task ID
- continue with next unblocked task chain

## Blocker termination criteria

Stop and escalate when:

- all currently unblocked chains are exhausted;
- same unresolved decision blocks 3 tasks in one chain;
- same blocker appears across 3 independent chains;
- required file or dependency is outside repository scope.

## Finalization rules

Before claiming Wave1 readiness:

- all owner tasks in validation matrix have evidence links
- readiness gates are updated from IN-PROGRESS to PASS with notes
- open issues are explicit and actionable
