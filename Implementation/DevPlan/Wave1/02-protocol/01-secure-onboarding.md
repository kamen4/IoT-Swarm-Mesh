# Wave 1 Protocol: Secure Onboarding

## Scope

Implement onboarding flow from Pending to Connected with VERIFY gate.

## Tasks (0.5-1 day each)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-PRO-001 | Model onboarding state machine | onboarding spec | state machine definition | Pending/Verified/Connected transitions documented | W1-FND-201 |
| W1-PRO-002 | Implement FIND/PONG discovery handling | onboarding + message registry | discovery handlers | target discovery succeeds in tests | W1-PRO-001 |
| W1-PRO-003 | Implement VERIFY handshake flow | onboarding + identity spec | verify handlers | successful and failed VERIFY paths tested | W1-PRO-002 |
| W1-PRO-004 | Implement PROTO/PROTO_R exchange path | onboarding + protocol docs | schema exchange handlers | protocol schema persisted after exchange | W1-PRO-003 |
| W1-PRO-005 | Implement START transition guard | onboarding state machine | transition guard | START only after VERIFY and PROTO_R | W1-PRO-004 |
| W1-PRO-006 | Add onboarding timeout/retry profile docs | integration handshake spec | profile documentation | timeout/retry values recorded in audit log | W1-PRO-005 |
| W1-PRO-007 | Implement connection-string parser validation | onboarding spec | parser validator | invalid connection strings rejected with reason | W1-PRO-001 |
| W1-PRO-008 | Add target-MAC match guard for PONG | onboarding + envelope specs | pong match guard | non-target PONG ignored safely | W1-PRO-002 |
| W1-PRO-009 | Implement VERIFY step-1 handling | onboarding + identity specs | verify-step1 handler | step-1 payload validated and persisted | W1-PRO-003 |
| W1-PRO-010 | Implement VERIFY step-2 handling | onboarding + identity specs | verify-step2 handler | step-2 state transitions verified | W1-PRO-009 |
| W1-PRO-011 | Implement VERIFY step-3 handling | onboarding + identity specs | verify-step3 handler | step-3 confirmation checks pass | W1-PRO-010 |
| W1-PRO-012 | Implement VERIFY step-4 finalization | onboarding + identity specs | verify-step4 handler | final step derives valid session state | W1-PRO-011 |
| W1-PRO-013 | Add S_PASSWORD persistence linkage checks | identity-security + persistence specs | persistence checks | derived secret is stored and retrievable by lifecycle flow | W1-PRO-012 |
| W1-PRO-014 | Add START precondition compliance tests | onboarding + handshake contract | precondition test suite | START blocked unless VERIFY and PROTO_R succeeded | W1-PRO-005 |
| W1-PRO-015 | Add onboarding negative-path regression tests | onboarding + error policy | negative test suite | failed VERIFY/invalid input paths remain safe | W1-PRO-013 |

## Source Pointers

- Implementation/Spec/02-protocol-core/03-onboarding.md
- Implementation/Spec/02-protocol-core/04-identity-security.md
- Implementation/Spec/04-integration/01-end-to-end-handshake.md
