# Server WP-02 Onboarding Pipeline

## Goal

Implement end-to-end onboarding orchestration from user onboarding artifact intake to connected state.

## Tasks

T1. Intake and pre-validation
- T1.1 Define onboarding artifact intake interface.
- T1.2 Define format validation and duplicate handling.

T2. Discovery flow orchestration
- T2.1 Define FIND issue logic.
- T2.2 Define PONG correlation handling.
- T2.3 Define timeout and retry policy.

T3. Verification flow orchestration
- T3.1 Define VERIFY step sequencing.
- T3.2 Define success/failure branch handling.
- T3.3 Define secure storage handoff for per-device secret.

T4. Protocol registration flow
- T4.1 Define PROTO request handling.
- T4.2 Define PROTO_R parse and validation.
- T4.3 Define START issuance criteria.

T5. Audit and observability
- T5.1 Define onboarding stage metrics.
- T5.2 Define onboarding failure taxonomy.

## Deliverables

- Onboarding pipeline specification.
- Failure and retry policy table.

## Acceptance criteria

- Onboarding flow can complete deterministically with auditable stage transitions.
