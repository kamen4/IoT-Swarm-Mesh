# Gateway Contract

## Role

Gateway is the bridge between ESP-NOW mesh side and HUB side.

## Responsibilities

- Receive mesh frames and pass them toward HUB transport layer.
- Receive HUB-originated commands and inject them into mesh with protocol envelope.
- Preserve gateway root identity consistency for routing convergence.

## Boundary Rules

- Gateway is transport bridge and routing root, not business-logic authority.
- Business state ownership remains in HUB services.

## Wave 1 Requirements

- MUST preserve frame metadata needed by routing and security verification.
- MUST support stable operation under continuous ingress and egress traffic.
- UART framing, checksum, and recovery behavior MUST follow Implementation/Spec/03-system/01b-gateway-uart-frame-spec.md.

## Source Pointers

- Protocol/_docs_v1.0/reference/architecture.md
- Implementation/Spec/03-system/01b-gateway-uart-frame-spec.md
