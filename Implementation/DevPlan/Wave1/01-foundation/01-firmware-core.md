# Wave 1 Foundation: Firmware Core

## Scope

Build the device-side protocol runtime foundation.

## Tasks (0.5-1 day each)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-FND-001 | Define firmware module layout | Spec protocol core files | module map | reviewed module map | none |
| W1-FND-002 | Implement envelope parsing skeleton | Spec envelope | parser module | parses known header fields | W1-FND-001 |
| W1-FND-003 | Implement envelope encode/decode tests | parser module | test suite | roundtrip tests pass | W1-FND-002 |
| W1-FND-004 | Add dedup cache structure | UP routing spec | dedup component | duplicate suppression test passes | W1-FND-002 |
| W1-FND-005 | Add ttl guard behavior | envelope + routing spec | ttl guard component | ttl zero drop verified | W1-FND-002 |
| W1-FND-006 | Add neighbor table primitives | UP/DOWN specs | neighbor table component | add/remove/lookup tests pass | W1-FND-001 |
| W1-FND-007 | Add local charge state holder | charge spec | charge state component | state persisted in runtime cycle | W1-FND-006 |
| W1-FND-008 | Prepare firmware diagnostic hooks | validation matrix | diagnostic outputs | required fields present in logs | W1-FND-003 |
| W1-FND-009 | Implement message type dispatcher stubs | message type registry | dispatch module | all known message types route to dedicated handlers | W1-FND-002 |
| W1-FND-010 | Implement msgId and seq primitives | envelope spec | id/seq utility | id/seq monotonic behavior verified in tests | W1-FND-002 |
| W1-FND-011 | Add ROUTING_HEADER field validation | envelope spec | header validator | ver/ttl/prevHopMac/charge fields validated per frame | W1-FND-003 |
| W1-FND-012 | Add SECURE_HEADER field validation | envelope spec | secure-header validator | dir/msgType/origin/dst/msgId/seq checks pass | W1-FND-003 |
| W1-FND-013 | Implement payload size guard (PAYLOAD_MAX) | byte-size constraints | payload bound checker | oversize payload is rejected and logged | W1-FND-011 |
| W1-FND-014 | Add drop-reason counters (ttl, dedup, parse) | validation matrix | drop metrics module | per-reason counters exported each cycle | W1-FND-008 |
| W1-FND-015 | Add dedup eviction policy tests | dedup component | eviction test suite | bounded cache evicts according to defined policy | W1-FND-004 |
| W1-FND-016 | Add ttl edge-case tests | ttl guard component | ttl test suite | ttl 0 and ttl 1 behaviors verified | W1-FND-005 |
| W1-FND-017 | Implement neighbor aging and stale removal | neighbor table component | neighbor aging routine | stale neighbors removed after configured threshold | W1-FND-006 |
| W1-FND-018 | Add charge state snapshot export | charge state + diagnostics | snapshot payload | q_up_self and q_total_self visible in diagnostics | W1-FND-007 |

## Source Pointers

- Implementation/Spec/02-protocol-core/01-envelope.md
- Implementation/Spec/02-protocol-core/05-up-routing.md
- Implementation/Spec/02-protocol-core/06-down-routing.md
- Implementation/Spec/02-protocol-core/07-charge-decay.md
