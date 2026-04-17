# Gateway Specification

## Functional scope

- Bridge UART and ESP-NOW protocol frames.
- Participate in routing and forwarding logic.
- Maintain local runtime state needed for forwarding correctness.

## Required runtime capabilities

- Envelope parse/build for bridge direction.
- TTL processing and dedup handling.
- Neighbor observations needed for routing decisions.
- Queueing for ingress/egress with backpressure behavior.

## Constraints

- Bounded memory for neighbor table, dedup cache, and forward queues.
- Deterministic behavior under queue pressure.
