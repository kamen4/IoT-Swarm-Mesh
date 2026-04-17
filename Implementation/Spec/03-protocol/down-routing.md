# DOWN Routing Contract

## Required behavior

- Direction: gateway to endpoint.
- Forwarding model: charge-induced tree, single-parent relation for eligible nodes.
- Parent condition: strictly higher charge than current node.
- Forwarding restriction: children-only propagation, never back to parent.

## Eligibility and thresholding

- Use q_forward to define forward-eligible subset for tree propagation.

## Safety properties from theorem context

- Loop-free forwarding under stated assumptions.
- Duplicate-free tree-broadcast under stated assumptions.

## OPEN DECISIONS

- Parent switch timing and hysteresis policy details where values are not fixed.
- Cold-start and transient behavior handling details not fully normative in docs.
