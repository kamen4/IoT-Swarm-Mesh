# DOWN Routing Specification

## Objective

Define loop-safe and duplicate-safe DOWN delivery over charge-induced parent/child structure.

## Parent Selection

- Each node selects a single parent using q_total improvement criteria and hysteresis.
- Parent switching SHOULD be conservative to avoid flapping.

## Forwarding Eligibility

- DOWN forwarding to subtree is restricted to nodes meeting q_forward threshold.
- Unknown unicast and broadcast behaviors follow tree-first semantics for eligible children.

## Delivery Constraints

- DOWN delivery should avoid loops and duplicate fan-out under theorem assumptions.
- ACK from destination is used as delivery completion signal toward gateway.

## Parent Loss and Re-parenting

- Requirement: Node MUST expire parent after configured parent-dead timeout when no parent signal is observed.
- Requirement: After parent expiry, node MUST re-run parent selection using current q_total observations and hysteresis policy.
- Requirement: During parent-loss recovery, node MUST continue dedup and ttl guard behavior and MUST NOT forward to stale parent.
- Requirement: Recovery path MUST be observable through parent-switch and convergence diagnostics.

## Theorem Link

- DOWN tree properties and assumptions are defined in theorem documentation.
- Implementation validation MUST track theorem assumptions operationally.

## Source Pointers

- Protocol/_docs_v1.0/algorithms/04-down-routing.md
- Protocol/_docs_v1.0/math/theorem.md
