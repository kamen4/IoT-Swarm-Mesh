# Constraints From Source Docs

## Resource constraints

- Neighbor table is bounded.
- Dedup cache is bounded.
- Forward queue is bounded.
- Frame payload size is bounded by envelope overhead.

## Behavioral constraints

- UP is top-1 forwarding oriented.
- DOWN relies on tree assumptions for guarantees.
- Mesh-control messages are untrusted hints.

## Operational constraints

- RF loss and interference are expected.
- UART can become bottleneck under burst traffic.
- Sleepy devices require pull-based handling.
