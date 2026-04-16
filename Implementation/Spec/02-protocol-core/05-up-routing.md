# UP Routing Specification

## Objective

Define deterministic UP forwarding behavior toward gateway using charge-based best-neighbor selection.

## Forwarding Rules

- Select one best eligible neighbor using UP quality signal.
- Exclude previous hop to reduce immediate loops.
- Decrement ttl each hop and drop when ttl reaches zero.

## Charge Interaction

- Forwarded UP traffic contributes to local charge accumulation.
- Advertised charge is carried through routing metadata for neighbor learning.

## Dedup Constraint

- Each node MUST suppress duplicates using bounded dedup cache keyed by (originMac, msgId).

## Determinism

- If multiple neighbors are equivalent, apply deterministic tie-break policy and keep it stable.

## Source Pointers

- Protocol/_docs_v1.0/algorithms/03-up-routing.md
