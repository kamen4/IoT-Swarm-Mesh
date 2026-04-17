# Server WP-01 Core Domain Model

## Goal

Build a complete server-side domain model aligned with documented protocol lifecycle.

## Tasks

T1. Device lifecycle domain
- T1.1 Define state transitions: Pending -> Verified -> Connected.
- T1.2 Define optional administrative states and transition guards.
- T1.3 Define invalid transition handling.

T2. Message correlation domain
- T2.1 Define correlation keys using protocol identifiers.
- T2.2 Define command state model and retry state model.

T3. Interaction-protocol domain
- T3.1 Define storage model for PROTO_R element metadata.
- T3.2 Define lookup and validation rules for IO operations.

T4. Persistence model
- T4.1 Define logical entities for users, devices, onboarding sessions, commands, and telemetry events.
- T4.2 Define retention boundary for command and telemetry history.
- T4.3 Define archival and cleanup policy boundaries.

T5. Domain validation rules
- T5.1 Define required invariants per lifecycle state.
- T5.2 Define rejection conditions and audit requirements.

## Deliverables

- Domain model specification.
- State transition table.
- Validation rule catalogue.

## Acceptance criteria

- All onboarding and command flows can be expressed with no ambiguous state handling.
