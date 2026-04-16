# End-to-End Handshake Contract

## Purpose

Define integration contract from registration to Connected state.

## Contract Steps

- Requirement: Registration input is accepted before mesh discovery starts.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Requirement: FIND/PONG discovery is executed for target MAC.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Requirement: VERIFY handshake is completed before protocol operation.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Requirement: Device protocol schema exchange uses PROTO and PROTO_R.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Requirement: START transitions device to Connected state.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md

## Contract Invariants

- Requirement: Connected state MUST NOT be set before successful VERIFY.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Requirement: Failed VERIFY handshake MUST keep lifecycle state below Connected.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md

## Timeout and Retry Profile (Wave 1)

- Requirement: Onboarding timeout and retry behavior MUST follow Implementation/DevPlan/bootstrap-timeout-retry-decision.md.
  - Source: Implementation/DevPlan/bootstrap-timeout-retry-decision.md

| Phase | Timeout | Retry Budget | Retry Schedule | Failure Action |
| --- | --- | --- | --- | --- |
| FIND/PONG discovery | 3 s | 5 attempts | linear +1 s | keep device in Pending and record blocker event |
| VERIFY step 1->2 | 5 s | 3 attempts | exponential (1 s, 2 s, 4 s) | abort onboarding attempt, keep below Connected |
| VERIFY step 3->4 | 5 s | 3 attempts | exponential (1 s, 2 s, 4 s) | abort onboarding attempt, keep below Connected |
| PROTO/PROTO_R | 5 s | 3 attempts | linear +2 s | keep state at Verified and schedule delayed retry |
| START delivery | 3 s | 2 attempts | linear +2 s | keep state at Verified and log start-pending flag |

## Source Pointers

- Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Protocol/_docs_v1.0/reference/protocol.md
- Implementation/DevPlan/bootstrap-timeout-retry-decision.md
