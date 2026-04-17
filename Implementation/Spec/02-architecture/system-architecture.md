# System Architecture

## Layered architecture

1. Host layer (on-premises)
- Containerized services including business server, queue, storage, monitoring, and Telegram integration.

2. Gateway layer
- Single or designated gateway ESP device bridging host UART traffic and mesh frames.

3. Mesh layer
- ESP devices participating in UP and DOWN routing as defined by protocol algorithms.

## Responsibility split

- Host layer owns business state, onboarding workflow, and user authorization.
- Gateway layer owns bridge behavior and runtime forwarding participation.
- Mesh devices own endpoint behavior and local forwarding decisions.

## Required architecture properties

- Deterministic flow decomposition by message type.
- Explicit backpressure handling on UART and forwarding queues.
- Observability points for command path and telemetry path.
