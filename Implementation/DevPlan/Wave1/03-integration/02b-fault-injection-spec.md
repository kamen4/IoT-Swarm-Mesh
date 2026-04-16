# Wave 1 Integration: Fault Injection Specification

## Purpose

Define deterministic fault injection behavior for integration harness scenarios.

## Supported Fault Types

- DROP: frame is removed before forwarding
- MALFORM: frame bytes are mutated to violate parser or checksum
- TIMEOUT: expected response is delayed beyond configured timeout
- DELAY: deterministic delay is inserted but within timeout budget

## Injector API Contract

- registerFault(scenarioId, faultType, targetStage, triggerIndex)
- clearFaults(scenarioId)
- runScenarioWithFaults(scenarioId)
- exportFaultReport(scenarioId)

## Determinism Rules

- Fault selection MUST be seed-driven.
- Same seed + scenario input MUST produce same fault trace.
- Fault report MUST include task-id, seed, scenarioId, fault list, and pass-fail.

## Validation Linkage

- Primary owner task: W1-INT-111
- Related tasks: W1-INT-114, W1-INT-115, W1-INT-116

## Source Pointers

- Implementation/DevPlan/Wave1/03-integration/02-integration-harness.md
- Implementation/Spec/04-integration/03-error-handling-policy.md
- Protocol/_docs_v1.0/mitigations/corner-cases.md
