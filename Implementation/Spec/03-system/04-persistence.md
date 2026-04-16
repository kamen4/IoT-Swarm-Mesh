# Persistence Requirements

## Objective

Define required persisted state for lifecycle continuity and protocol correctness.

## Required Entities

- Device identity record (MAC and onboarding status).
- Device lifecycle state (Pending, Verified, Connected).
- Derived per-device credential material reference (S_PASSWORD handling policy).
- Device protocol schema metadata received via PROTO_R.

## Operational Requirements

- Lifecycle state MUST survive service restart.
- Persistence updates MUST remain consistent with onboarding transition order.
- Sensitive credential material handling MUST follow security contract.
- Persistence timestamps MUST use UTC and include millisecond precision.
- Failed onboarding transitions MUST roll back atomically to last valid lifecycle state.

## Source Pointers

- Protocol/_docs_v1.0/reference/architecture.md
- Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Protocol/_docs_v1.0/algorithms/05-identity-security.md
