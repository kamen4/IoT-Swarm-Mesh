# Wave 1 Protocol: UP Routing

## Scope

Implement UP forwarding behavior and route-guard mechanics.

## Tasks (0.5-1 day each)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-PRO-101 | Implement best-neighbor candidate selection | up-routing spec | candidate selector | selector returns deterministic candidate list | W1-FND-006 |
| W1-PRO-102 | Implement previous-hop exclusion | up-routing spec | exclusion filter | previous hop excluded in forwarding tests | W1-PRO-101 |
| W1-PRO-103 | Implement ttl decrement and zero-drop | envelope spec | ttl guard in route path | ttl tests pass for edge cases | W1-FND-005 |
| W1-PRO-104 | Integrate dedup cache into UP pipeline | up-routing + firmware dedup | dedup-enabled forwarding | duplicate suppression verified | W1-FND-004 |
| W1-PRO-105 | Implement charge advertisement update path | charge-decay spec | charge propagation updates | q_up-related updates visible in diagnostics | W1-FND-007 |
| W1-PRO-106 | Add UP tie-break determinism tests | up-routing spec | deterministic test suite | tie cases stable across repeated runs | W1-PRO-101 |
| W1-PRO-107 | Add neighbor stale-entry expiration | up-routing + neighbor table | stale expiration routine | stale neighbors removed according to policy | W1-PRO-101 |
| W1-PRO-108 | Implement candidate scoring utility for q_up | up-routing spec | scoring utility | scores are deterministic for same inputs | W1-PRO-101 |
| W1-PRO-109 | Add strict previous-hop exclusion tests | forwarding contract | exclusion test suite | no forwarding to immediate previous hop | W1-PRO-102 |
| W1-PRO-110 | Implement no-eligible-neighbor fallback behavior | up-routing + error policy | fallback handler | fallback path documented and tested for dead-end cases | W1-PRO-108 |
| W1-PRO-111 | Add advertised-charge frame read/write checks | envelope + up-routing specs | charge frame tests | charge metadata roundtrip verified | W1-PRO-105 |
| W1-PRO-112 | Add q_up_self and q_total_self increment tests | up-routing + charge specs | increment test suite | expected increments occur on forwarded UP traffic | W1-PRO-105 |
| W1-PRO-113 | Add dedup+ttl combined stress tests | up-routing + envelope specs | stress test suite | duplicate and ttl guards remain stable under burst load | W1-PRO-104 |
| W1-PRO-114 | Add UP failure-recovery alternate path tests | forwarding contracts | recovery test suite | alternate-forward behavior works when primary forward fails | W1-PRO-110 |
| W1-PRO-115 | Export UP routing metrics bundle | observability minimum spec | UP metrics payload | candidate count, drop reasons, and success counts exported | W1-PRO-113 |

## Source Pointers

- Implementation/Spec/02-protocol-core/05-up-routing.md
- Implementation/Spec/02-protocol-core/01-envelope.md
- Implementation/Spec/04-integration/02-forwarding-contracts.md
