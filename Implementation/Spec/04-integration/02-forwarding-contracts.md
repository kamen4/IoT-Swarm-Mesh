# Forwarding Contracts

## Purpose

Define integration-level forwarding guarantees for UP and DOWN paths.

## UP Contract

- UP forwarding uses best-neighbor selection.
- ttl and dedup guards MUST be applied at every hop.
- Previous hop exclusion MUST be respected.

## DOWN Contract

- DOWN forwarding follows parent/child tree semantics.
- q_forward eligibility threshold gates child forwarding.
- Destination ACK is the delivery completion signal.

## Operational Contract

- Forwarding decisions MUST remain deterministic under equal-score ties.
- Forwarding path behavior MUST remain compatible with theorem assumptions used in validation.

## Source Pointers

- Protocol/_docs_v1.0/algorithms/03-up-routing.md
- Protocol/_docs_v1.0/algorithms/04-down-routing.md
