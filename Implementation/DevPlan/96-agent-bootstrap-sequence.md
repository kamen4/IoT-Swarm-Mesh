# Agent Bootstrap Sequence

This file defines the mandatory first-run sequence for autonomous execution.

## Stage 0: unblock execution governance

| Step | Action | Output |
| --- | --- | --- |
| B0-001 | Resolve timeout/retry profile decision for onboarding phases | bootstrap-timeout-retry-decision.md |
| B0-002 | Update handshake spec with selected timeout/retry profile reference | updated integration spec and source pointers |
| B0-003 | Align W1-PRO-006 with finalized decision record | updated secure-onboarding task notes |
| B0-004 | Run consistency audit for handshake and onboarding files | audit entry with pass/fail |

## Stage 1: prepare evidence workflow

| Step | Action | Output |
| --- | --- | --- |
| B1-001 | Validate owner-task links in validation matrix | matrix check note in audit log |
| B1-002 | Prepare evidence artifact path convention | evidence-artifact-convention.md |
| B1-003 | Add evidence capture checklist to execution workflow | updated execution protocol with evidence convention reference |
| B1-004 | Run consistency audit for validation and readiness docs | audit entry with pass/fail |

## Stage 2: start full-product execution

| Step | Action | Output |
| --- | --- | --- |
| B2-001 | Start unblocked tasks from 95-full-product-master-plan.md | first completed FP task batch |
| B2-002 | Apply post-batch updates to validation/risk/audit/readiness docs | synchronized quality files |
| B2-003 | Continue batched execution until final handoff gate | completed implementation and handoff artifacts |

## Source Pointers

- Implementation/DevPlan/95-full-product-master-plan.md
- Implementation/DevPlan/94-execution-protocol.md
- Implementation/DevPlan/92-audit-log.md
- Implementation/DevPlan/bootstrap-timeout-retry-decision.md
- Implementation/DevPlan/evidence-artifact-convention.md
- Implementation/Spec/04-integration/01-end-to-end-handshake.md
