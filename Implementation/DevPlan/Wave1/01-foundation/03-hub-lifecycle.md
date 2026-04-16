# Wave 1 Foundation: HUB Lifecycle

## Scope

Implement hub-side lifecycle ownership and onboarding state persistence.

## Tasks (0.5-1 day each)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-FND-201 | Define lifecycle state model mapping | onboarding spec | state model | Pending/Verified/Connected mapping approved | none |
| W1-FND-202 | Implement lifecycle persistence schema | persistence spec | schema migration | schema stores required entities | W1-FND-201 |
| W1-FND-203 | Implement transition guards | onboarding + persistence spec | transition service | invalid transitions blocked | W1-FND-202 |
| W1-FND-204 | Implement onboarding state event logging | hub contract | event log hooks | transition audit logs present | W1-FND-203 |
| W1-FND-205 | Implement restart recovery behavior | persistence spec | recovery routine | restart resumes consistent state | W1-FND-202 |
| W1-FND-206 | Add role-gated lifecycle admin operations | rbac spec | guarded admin handlers | role checks verified | W1-FND-203 |
| W1-FND-207 | Add lifecycle diagnostics export | validation matrix | lifecycle metrics | required lifecycle metrics exported | W1-FND-204 |
| W1-FND-208 | Implement Pending record creation flow | onboarding spec + persistence | pending creation routine | new device enters Pending with required fields | W1-FND-202 |
| W1-FND-209 | Implement Verified transition transaction | onboarding + identity specs | verified transition routine | transition is atomic and persisted | W1-FND-203 |
| W1-FND-210 | Implement Connected transition transaction | onboarding spec | connected transition routine | Connected set only after required preconditions | W1-FND-209 |
| W1-FND-211 | Add failed VERIFY rollback path | onboarding + error policy | rollback routine | failed VERIFY restores safe non-connected state | W1-FND-209 |
| W1-FND-212 | Add PROTO_R schema persistence checks | onboarding + persistence specs | schema validation routine | schema persisted with integrity checks | W1-FND-210 |
| W1-FND-213 | Add restart state restore tests | persistence + lifecycle model | restore test suite | restart restores valid state and transitions | W1-FND-205 |
| W1-FND-214 | Add lifecycle transition audit links | hub contract + diagnostics | audit linkage fields | each transition mapped to traceable audit record | W1-FND-204 |
| W1-FND-215 | Add role enforcement regression tests | rbac spec | rbac regression suite | unauthorized lifecycle actions rejected | W1-FND-206 |
| W1-FND-216 | Add lifecycle consistency scan job | lifecycle model + diagnostics | consistency scanner | invalid state combinations detected and reported | W1-FND-213 |

## Source Pointers

- Implementation/Spec/03-system/02-hub-contract.md
- Implementation/Spec/03-system/04-persistence.md
- Implementation/Spec/03-system/03-rbac-and-users.md
- Protocol/_docs_v1.0/algorithms/01-onboarding.md
