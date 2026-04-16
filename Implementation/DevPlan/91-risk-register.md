# Risk Register

## Fields

- risk-id
- trigger
- impact
- mitigation-task-id
- verification-signal

## Wave 1 Risks

| Risk ID | Trigger | Impact | Mitigation Task ID | Verification Signal |
| --- | --- | --- | --- | --- |
| R-W1-001 | neighbor table saturation | unstable forwarding paths | W1-FND-006 | neighbor eviction metrics stable |
| R-W1-002 | parent flapping under noisy links | DOWN instability | W1-PRO-204 | parent-change rate declines after hysteresis |
| R-W1-003 | stale charge dominance | convergence slowdown | W1-PRO-206 | decay epochs reduce stale charge spread |
| R-W1-004 | duplicate amplification | bandwidth waste and loops | W1-FND-004 | dedup hit ratio increases during burst tests |
| R-W1-005 | malformed frame ingress | runtime instability | W1-FND-003 | frame conformance tests pass |
| R-W1-006 | onboarding verification failures | device never reaches Connected | W1-PRO-003 | failed verify path keeps state below Connected |
| R-W1-007 | baseline drift | non-reproducible behavior | W1-PAR-005 | baseline lock tests pass |
| R-W1-008 | insufficient audit coverage | unmanaged requirement drift | W1-INT-202 | audit log completeness reaches 100 percent |

## Source Pointers

- Protocol/_docs_v1.0/mitigations/corner-cases.md
- Protocol/_docs_v1.0/mitigations/convergence-tuning.md
- Implementation/DevPlan/90-validation-matrix.md
