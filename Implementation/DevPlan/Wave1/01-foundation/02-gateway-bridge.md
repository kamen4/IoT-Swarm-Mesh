# Wave 1 Foundation: Gateway Bridge

## Scope

Implement gateway-side bridge responsibilities between mesh and hub layers.

## Tasks (0.5-1 day each)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-FND-101 | Define gateway ingress and egress boundaries | gateway contract | boundary spec note | boundary review complete | none |
| W1-FND-102 | Implement mesh-to-hub frame pass-through | envelope + gateway contract | ingress pipeline | valid frames forwarded | W1-FND-101 |
| W1-FND-103 | Implement hub-to-mesh command injection | gateway + protocol docs | egress pipeline | commands reach mesh format | W1-FND-101 |
| W1-FND-104 | Preserve routing/security metadata on bridge | envelope spec | metadata mapping layer | field preservation test passes | W1-FND-102 |
| W1-FND-105 | Add gateway root identity consistency checks | down routing spec | identity check hook | root identity stable in logs | W1-FND-103 |
| W1-FND-106 | Add gateway bridge diagnostics | validation matrix | diagnostic records | required bridge events logged | W1-FND-104 |
| W1-FND-107 | Implement ingress queue bound control | error policy + corner-cases | ingress queue guard | ingress queue respects configured hard limit | W1-FND-102 |
| W1-FND-108 | Implement egress queue bound control | error policy + corner-cases | egress queue guard | egress queue respects configured hard limit | W1-FND-103 |
| W1-FND-109 | Add malformed frame rejection at bridge boundary | envelope spec | bridge parser guard | malformed frames dropped without pipeline crash | W1-FND-104 |
| W1-FND-110 | Add BEACON forwarding hook | down-routing spec | beacon hook | gateway emits/forwards BEACON with expected metadata | W1-FND-105 |
| W1-FND-111 | Add DECAY forwarding hook | charge-decay spec | decay hook | decay epoch payload reaches mesh side correctly | W1-FND-110 |
| W1-FND-112 | Add WAKE forwarding handling path | down-routing + identity/security | wake handler | WAKE frames passed with expected mesh-control policy | W1-FND-109 |
| W1-FND-113 | Implement ACK stop-condition bridge handling | down-routing spec | ack stop handler | gateway command retry loop stops when ACK observed | W1-FND-103 |
| W1-FND-114 | Add overload diagnostics and alarms | validation matrix | overload metrics | queue depth and drop bursts emitted to diagnostics | W1-FND-107 |

## Source Pointers

- Implementation/Spec/03-system/01-gateway-contract.md
- Implementation/Spec/02-protocol-core/01-envelope.md
- Protocol/_docs_v1.0/reference/architecture.md
