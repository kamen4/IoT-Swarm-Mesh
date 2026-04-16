# Evidence Artifact Convention

## Purpose

Define a deterministic evidence storage and linking convention for validation closure.

## Storage Layout

All evidence artifacts MUST be stored under:

- Implementation/evidence/sim/
- Implementation/evidence/hw/
- Implementation/evidence/integration/
- Implementation/evidence/perf/
- Implementation/evidence/audit/

## Naming Contract

File name MUST follow:

TASKID_YYYY-MM-DD_HHMMSS_slug.ext

Examples:

- W1-PRO-003_2026-04-17_141500_onboarding-success.log
- W1-INT-104_2026-04-17_151020_down-delivery-report.json
- FP-QA-006_2026-04-20_103200_resilience-summary.md

## Required Metadata

Every evidence artifact MUST include:

- task-id
- timestamp-utc
- scenario-or-test-name
- pass-fail
- tool-or-harness-version

If format does not support metadata fields directly, add a sidecar .meta.json with the same base name.

## Validation Matrix Link Format

When attaching evidence in validation matrix, use markdown links to repository-relative paths:

- [sim evidence](Implementation/evidence/sim/W1-FND-003_2026-04-17_130100_frame-roundtrip.log)
- [hw evidence](Implementation/evidence/hw/W1-PRO-203_2026-04-17_172300_tree-forwarding.csv)

## Audit Log Link Format

For each batch closure in audit log, include:

- evidence-links: comma-separated markdown links
- owner-task-id: task or task batch owner

## Source Pointers

- Implementation/DevPlan/90-validation-matrix.md
- Implementation/DevPlan/94-execution-protocol.md
- Implementation/DevPlan/96-agent-bootstrap-sequence.md
