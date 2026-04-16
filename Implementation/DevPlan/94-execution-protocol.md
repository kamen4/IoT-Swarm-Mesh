# Execution Protocol

This file defines how to execute Wave1 tasks from DevPlan with consistent evidence and closure.

## Step-by-step workflow

1. Select next unblocked task set.
2. Confirm dependency IDs exist and are complete.
3. Execute implementation work for selected tasks.
4. Run relevant checks and collect evidence.
5. Update validation matrix evidence links.
6. Update risk signals if mitigation tasks were touched.
7. Append audit entry with pass/fail and fix links.
8. Update readiness checklist fields impacted by the batch.

## Full-Product (FP-*) execution governance

The full-product phase uses the same workflow and quality discipline as Wave1, with FP-specific tracking:

- use FP-* task IDs from the task ID registry;
- add validation ownership and evidence links for FP tracks in validation matrix;
- add FP mitigation mappings in risk register;
- maintain FP completion state in readiness checklist and audit log.

Mandatory update targets remain unchanged:

- Implementation/DevPlan/90-validation-matrix.md
- Implementation/DevPlan/91-risk-register.md
- Implementation/DevPlan/92-audit-log.md
- Implementation/DevPlan/99-readiness-checklist.md

## Evidence minimum

For each closed task batch include:

- artifact link
- test or check result link
- metric or log link when applicable
- evidence paths and file names compliant with Implementation/DevPlan/evidence-artifact-convention.md

## Gate progression rules

- Do not set PASS without evidence links.
- Keep IN-PROGRESS if any owner-task evidence is missing.
- Keep OPEN if a required decision is still PROVISIONAL.

## Mandatory update targets

- Implementation/DevPlan/90-validation-matrix.md
- Implementation/DevPlan/91-risk-register.md
- Implementation/DevPlan/92-audit-log.md
- Implementation/DevPlan/99-readiness-checklist.md
- Implementation/DevPlan/evidence-artifact-convention.md

## Source Pointers

- Implementation/DevPlan/93-task-id-registry.md
- Implementation/DevPlan/Wave1/03-integration/03-dependency-map.md
- Implementation/DevPlan/90-validation-matrix.md
- Implementation/DevPlan/99-readiness-checklist.md
- Implementation/DevPlan/evidence-artifact-convention.md
