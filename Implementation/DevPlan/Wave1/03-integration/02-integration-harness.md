# Wave 1 Integration: Integration Harness

## Scope

Create repeatable integration harness for onboarding and routing verification.

## Tasks (0.5-1 day each)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-INT-101 | Define harness scenarios for onboarding and forwarding | integration specs | scenario set | scenario list approved | W1-PRO-005 |
| W1-INT-102 | Implement 3-node onboarding scenario | onboarding tasks | scenario runner | Pending->Connected path verified | W1-INT-101 |
| W1-INT-103 | Implement multi-hop UP scenario | up-routing tasks | UP scenario runner | dedup and ttl checks observed | W1-INT-101 |
| W1-INT-104 | Implement DOWN delivery scenario | down-routing tasks | DOWN scenario runner | child forwarding and ACK completion verified | W1-INT-101 |
| W1-INT-105 | Add negative-case scenario set | error policy | failure scenario suite | malformed/failed verify scenarios handled safely | W1-INT-102 |
| W1-INT-106 | Integrate baseline snapshot checks in harness | params baseline | baseline assert hooks | run fails on baseline drift | W1-INT-102 |
| W1-INT-107 | Add scenario evidence export | validation matrix | evidence artifacts | each run writes evidence links | W1-INT-103 |
| W1-INT-108 | Add deterministic rerun check | harness runs | rerun consistency report | repeated run output stable under same input | W1-INT-107 |
| W1-INT-109 | Add topology fixture generator | dependency map + forwarding specs | topology fixture set | repeatable small/medium/large topology fixtures produced | W1-INT-101 |
| W1-INT-110 | Add shared assertion helper library | harness scenarios | assertion helpers | assertions provide clear expected/actual diagnostics | W1-INT-102 |
| W1-INT-111 | Add fault injection support (drop, malformed, timeout) | error policy + harness | fault injector | deterministic fault insertion available per scenario | W1-INT-105 |
| W1-INT-112 | Add scenario setup/teardown lifecycle control | harness scenarios | setup/teardown manager | no residual state after each scenario run | W1-INT-102 |
| W1-INT-113 | Add baseline throughput/latency measurement scenario | validation strategy | performance baseline report | baseline throughput and latency exported | W1-INT-107 |
| W1-INT-114 | Add payload boundary scenarios near PAYLOAD_MAX | envelope/byte-size specs | payload boundary runner | min/nominal/max payload cases pass | W1-INT-103 |
| W1-INT-115 | Add scenario trace capture hooks | validation matrix | trace capture artifacts | traces include routing and lifecycle transitions | W1-INT-107 |
| W1-INT-116 | Add rerun trace equivalence checker | deterministic rerun rules | trace equivalence report | repeated identical runs show stable trace output | W1-INT-108 |
| W1-INT-117 | Add requirement-coverage scenario matrix | source-map + validation strategy | coverage matrix | each critical requirement mapped to one or more scenarios | W1-INT-105 |
| W1-INT-118 | Add baseline-drift failure scenario | params baseline + harness | drift-failure scenario | harness run fails and reports when baseline drift detected | W1-INT-106 |

## Source Pointers

- Implementation/Spec/04-integration/01-end-to-end-handshake.md
- Implementation/Spec/04-integration/02-forwarding-contracts.md
- Implementation/Spec/04-integration/03-error-handling-policy.md
- Implementation/DevPlan/Wave1/02-protocol/04-params-baseline.md
- Implementation/DevPlan/Wave1/03-integration/02b-fault-injection-spec.md
