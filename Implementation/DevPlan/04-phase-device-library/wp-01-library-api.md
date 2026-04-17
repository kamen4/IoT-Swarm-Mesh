# Device Library WP-01 Library API Surface

## Goal

Define stable and minimal API boundaries required by endpoint firmware.

## Tasks

T1. API domain map
- T1.1 Define onboarding API entry points.
- T1.2 Define message parse/build API entry points.
- T1.3 Define routing participation API entry points.
- T1.4 Define interaction-protocol API entry points.

T2. Runtime callback contracts
- T2.1 Define IO read callback contract.
- T2.2 Define IO write callback contract.
- T2.3 Define queue-pressure callback contract.

T3. Error model
- T3.1 Define error categories and propagation rules.
- T3.2 Define recoverable vs fatal error handling boundaries.

## Deliverables

- Library API contract specification.
- Callback and error model specification.

## Acceptance criteria

- Endpoint firmware teams can integrate library without undefined behavior.
