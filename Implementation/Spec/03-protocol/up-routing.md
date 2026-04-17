# UP Routing Contract

## Required behavior

- Direction: endpoint toward gateway.
- Candidate choice: best-neighbor forwarding (top-1) by charge estimate.
- Must exclude prevHop from immediate bounce-back.
- Must update ROUTING_HEADER fields per hop.

## State

- Local q_up style score.
- Neighbor score estimates learned from observed traffic.
- Dedup cache on (originMac, msgId).

## Stability constraints

- TTL decrement on each forward.
- Drop on TTL exhaustion.
- Prevent forwarding loops using dedup + ttl + candidate filtering.

## OPEN DECISIONS

- Exact tie-break order when equal charge is observed.
- Exact local update function when source docs leave it implementation-defined.
