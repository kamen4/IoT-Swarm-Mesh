---
description: Load these rules when editing Implementation/DevPlan files, including task expansion, dependency updates, and readiness tracking.
applyTo: "Implementation/DevPlan/**"
name: "Implementation DevPlan Authoring"
---

# DevPlan authoring instructions

## Scope

Use this file for all planning and execution decomposition work in Implementation/DevPlan.

## Task table contract

Each task row must keep this schema:

- Task ID
- Objective
- Inputs
- Outputs
- Done Criteria
- Dependencies

## Granularity and quality

- Target 0.5 to 1 day per task.
- Keep objective concrete and testable.
- Done criteria must be measurable.
- Dependencies must reference existing task IDs only.

## Task ID ranges

Use stable ranges to avoid collisions:

- W1-FND-001..099 firmware foundation
- W1-FND-100..199 gateway foundation
- W1-FND-200..299 hub foundation
- W1-PRO-001..099 secure onboarding
- W1-PRO-100..199 UP routing
- W1-PRO-200..299 DOWN routing
- W1-PAR-001..099 baseline parameters
- W1-INT-100..199 integration harness
- W1-INT-200..299 observability and closure

## Mandatory synchronized updates

When tasks are added, removed, or renumbered, update in the same change:

- Implementation/DevPlan/Wave1/03-integration/03-dependency-map.md
- Implementation/DevPlan/90-validation-matrix.md
- Implementation/DevPlan/91-risk-register.md
- Implementation/DevPlan/99-readiness-checklist.md
- Implementation/DevPlan/93-task-id-registry.md

## Task registry rule

- Maintain Implementation/DevPlan/93-task-id-registry.md as live registry of assigned IDs.
- For each add/remove/renumber action, update usage snapshot and keep ranges collision-free.

## Validation ownership rule

- Each validation row owner task ID must exist.
- Evidence attachment requirements must remain aligned with owner tasks.

## Risk linkage rule

- Each risk mitigation task ID must exist.
- Verification signal must map to an observable artifact or metric.

## Prohibited actions

- No dangling dependencies.
- No duplicate task IDs.
- No vague placeholders in Done Criteria.
