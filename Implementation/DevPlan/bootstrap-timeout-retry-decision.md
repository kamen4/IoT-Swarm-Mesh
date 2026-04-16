# Bootstrap Timeout and Retry Decision (B0-001)

## Purpose

Lock onboarding timeout and retry profile to unblock autonomous execution.

## Decision Table

| Phase | Timeout | Retry Budget | Retry Schedule | Failure Action |
| --- | --- | --- | --- | --- |
| FIND/PONG discovery | 3 s | 5 attempts | linear +1 s | keep device in Pending and record blocker event |
| VERIFY step 1->2 | 5 s | 3 attempts | exponential (1 s, 2 s, 4 s) | abort onboarding attempt, keep below Connected |
| VERIFY step 3->4 | 5 s | 3 attempts | exponential (1 s, 2 s, 4 s) | abort onboarding attempt, keep below Connected |
| PROTO/PROTO_R | 5 s | 3 attempts | linear +2 s | keep state at Verified and schedule delayed retry |
| START delivery | 3 s | 2 attempts | linear +2 s | keep state at Verified and log start-pending flag |

## Guardrails

- Requirement: Retry counters MUST reset only on successful phase completion.
- Requirement: Total onboarding wall-clock budget per attempt MUST be capped at 120 s.
- Requirement: All timeout and retry failures MUST emit audit event with device MAC, phase, attempt, and reason.

## Closure Links

- Applies to: Implementation/Spec/04-integration/01-end-to-end-handshake.md
- Related task: W1-PRO-006
- Bootstrap step: B0-001

## Source Pointers

- Implementation/DevPlan/96-agent-bootstrap-sequence.md
- Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Protocol/_docs_v1.0/mitigations/corner-cases.md
