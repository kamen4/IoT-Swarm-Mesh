# HUB Contract

## Role

HUB coordinates onboarding, lifecycle, command flow, telemetry flow, and access control.

## Responsibilities

- Own device lifecycle state transitions.
- Orchestrate onboarding workflow.
- Process command/response and telemetry events.

## Service Boundary

- HUB services are deployed in on-prem architecture as documented.
- Integration among services follows the architecture contract and shared state model.

## Wave 1 Requirements

- MUST keep authoritative per-device lifecycle state.
- MUST keep onboarding and protocol operation auditable.

## Source Pointers

- Protocol/_docs_v1.0/reference/architecture.md
