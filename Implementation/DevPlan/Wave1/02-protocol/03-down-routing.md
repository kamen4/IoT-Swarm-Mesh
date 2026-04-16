# Wave 1 Protocol: DOWN Routing

## Scope

Implement parent/child delivery behavior and stability controls for DOWN traffic.

## Tasks (0.5-1 day each)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-PRO-201 | Implement parent selection by q_total improvement | down-routing spec | parent selector | parent assignment consistent in tests | W1-FND-006 |
| W1-PRO-202 | Implement q_forward eligibility gating | down-routing spec | eligibility filter | ineligible child forwarding blocked | W1-PRO-201 |
| W1-PRO-203 | Implement children-only forwarding for unknown unicast | forwarding contracts | down forwarder | child fan-out matches contract tests | W1-PRO-202 |
| W1-PRO-204 | Implement hysteresis and conservative switching | theorem + corner-cases | switch control logic | parent flapping rate reduced in stress tests | W1-PRO-201 |
| W1-PRO-205 | Implement ACK-based completion path | down-routing spec | ack completion handling | gateway stop condition triggered by ACK | W1-PRO-203 |
| W1-PRO-206 | Implement decay and convergence stabilization hooks | charge-decay spec | decay hook integration | decay epochs applied with expected cadence | W1-PRO-204 |
| W1-PRO-207 | Add theorem-observability checks for DOWN tree | theorem docs + validation strategy | theorem check outputs | assumptions visibility reported in diagnostics | W1-PRO-203 |
| W1-PRO-208 | Add parent-switch notification events | down-routing + diagnostics | switch notification events | children/diagnostics see parent-switch reason and timestamp | W1-PRO-204 |
| W1-PRO-209 | Implement DOWN loop-detection guard | down-routing + theorem | loop guard | loop signatures detected and dropped safely | W1-PRO-203 |
| W1-PRO-210 | Add per-child delivery confirmation counters | down-routing + ACK flow | child confirmation counters | ACK success rate tracked per child | W1-PRO-205 |
| W1-PRO-211 | Add per-child queue/rate limiter | down-routing + corner-cases | per-child limiter | child queues remain bounded under burst traffic | W1-PRO-202 |
| W1-PRO-212 | Add broadcast-vs-unicast decision tests | forwarding contracts | decision test suite | broadcast and unknown unicast paths match contract | W1-PRO-203 |
| W1-PRO-213 | Add ACK latency tracking for DOWN flows | down-routing + observability | ack latency metrics | ack latency exported with delivery counters | W1-PRO-210 |
| W1-PRO-214 | Add parent-switch cooldown enforcement tests | theorem + corner-cases | cooldown test suite | excessive switch oscillations are prevented | W1-PRO-204 |
| W1-PRO-215 | Add decay-epoch tree rebuild verification tests | charge-decay + down-routing specs | rebuild test suite | tree rebuild behavior verified after decay epochs | W1-PRO-206 |
| W1-PRO-216 | Export DOWN tree structure metrics | theorem observability + diagnostics | tree metrics payload | depth, fanout, and eligible-node counts exported | W1-PRO-207 |
| W1-PRO-217 | Add parent-loss recovery scenario tests | corner-cases + down-routing specs | parent-loss test suite | subtree recovery and re-parenting verified | W1-PRO-214 |

## Source Pointers

- Implementation/Spec/02-protocol-core/06-down-routing.md
- Implementation/Spec/02-protocol-core/07-charge-decay.md
- Protocol/_docs_v1.0/math/theorem.md
- Protocol/_docs_v1.0/mitigations/corner-cases.md
