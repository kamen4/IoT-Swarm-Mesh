# DevPlan Index

This folder contains execution-ready planning artifacts for Wave 1 implementation.

## Wave 1 Constraints

- Scope: protocol product only (device + gateway + hub)
- Baseline: cand2_more_inertia is fixed default profile
- Task granularity target: 0.5 to 1 day per task
- All tasks must link to at least one Spec file and one source pointer

## Wave 1 Navigation

- Wave1/01-foundation/01-firmware-core.md
- Wave1/01-foundation/02-gateway-bridge.md
- Wave1/01-foundation/03-hub-lifecycle.md
- Wave1/02-protocol/01-secure-onboarding.md
- Wave1/02-protocol/02-up-routing.md
- Wave1/02-protocol/03-down-routing.md
- Wave1/02-protocol/04-params-baseline.md
- Wave1/03-integration/01-observability-minimum.md
- Wave1/03-integration/02-integration-harness.md
- Wave1/03-integration/02b-fault-injection-spec.md
- Wave1/03-integration/03-dependency-map.md
- 90-validation-matrix.md
- 91-risk-register.md
- 92-audit-log.md
- 93-task-id-registry.md
- 94-execution-protocol.md
- bootstrap-timeout-retry-decision.md
- evidence-artifact-convention.md
- 95-full-product-master-plan.md
- 96-agent-bootstrap-sequence.md
- 97-agent-launch-checklist.md
- 98-full-product-agent-prompt.md
- 99-readiness-checklist.md

## Full-Product Launch Section

- bootstrap-timeout-retry-decision.md - locked onboarding timeout/retry profile
- evidence-artifact-convention.md - mandatory evidence storage and link format
- 95-full-product-master-plan.md - full-product execution plan by FP tracks
- 96-agent-bootstrap-sequence.md - mandatory first-run unblock and startup sequence
- 97-agent-launch-checklist.md - pre-launch checklist for autonomous run
- 98-full-product-agent-prompt.md - ready-to-run autonomous agent prompt

## Execution Order

1. Foundation files
2. Protocol files
3. Integration files
4. Validation/risk/audit artifacts
5. Readiness checklist closure

## Granularity Snapshot

- Foundation tasks: 48
- Protocol and baseline tasks: 61
- Integration tasks: 32
- Total Wave 1 micro-tasks: 141

## Source Pointers

- Implementation/Spec/00-index.md
- Implementation/Spec/00-source-map.md
- Protocol/_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json
