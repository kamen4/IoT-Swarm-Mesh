# Full Product Specification

## Objective

Define the complete product target: ESP device-side protocol implementation, gateway bridge, and dockerized HUB platform.

## Product Boundary

Full product includes:

1. Device protocol library for ESP nodes
2. Gateway firmware bridge between ESP-NOW mesh and UART
3. HUB stack in Docker containers
4. Business and control plane for onboarding, messaging, and device interaction
5. Storage, telemetry, dashboards, and role-based user control

## Architecture Requirements

### A. Device Library

- Device implementation MUST support protocol envelope fields and message registry from protocol specification.
- Device implementation MUST support onboarding flow including FIND/PONG, VERIFY handshake, PROTO/PROTO_R, and START.
- Device implementation MUST support UP and DOWN routing behavior as documented.
- Device implementation MUST support dedup and ttl guards.
- Device implementation MUST export diagnostics required by validation strategy.

### B. Gateway Firmware Bridge

- Gateway MUST bridge mesh traffic to HUB via UART.
- Gateway MUST preserve protocol metadata required for routing and security processing.
- Gateway MUST forward control messages required for convergence and wake signaling.
- Gateway MUST provide bounded queue behavior and overload-safe operation.

### C. HUB Docker Stack

HUB deployment MUST include dockerized services aligned with architecture reference:

- UART listener/sender service
- Redis pub/sub service
- ASP.NET business server
- SQL data store
- Influx TSDB
- Grafana dashboards
- Telegram server component

Service startup order, boundaries, and ownership rules MUST follow Implementation/Spec/03-system/05-hub-service-contracts.md.

### D. Business Server Behavior

- Server MUST be lifecycle authority for device states Pending, Verified, Connected.
- Server MUST execute onboarding sequence and persist resulting state.
- Server MUST handle command and telemetry exchange over protocol contracts.
- Server MUST enforce role rules from users specification.

### E. Data and Persistence

- SQL persistence MUST store users, devices, lifecycle state, and protocol metadata.
- Telemetry pipeline MUST store time-series operational events in TSDB.
- Persistence and telemetry records MUST be auditable from onboarding to runtime operations.

### F. Security and Identity

- Identity model MUST remain server-verified as specified.
- End-to-end authenticity MUST remain based on S_PASSWORD and HMAC model.
- Mesh-control messages MUST be processed as untrusted hints with guards.

### G. Observability

- Product MUST provide operator-visible metrics for lifecycle, routing stability, and delivery health.
- Product MUST expose evidence required by validation matrix and readiness gates.

## Delivery Acceptance Gates

A full-product delivery is accepted only when:

- all target components (device, gateway, HUB services) are implemented and integrated;
- onboarding and control flows work end-to-end through gateway and HUB stack;
- validation matrix evidence is attached for critical invariants;
- readiness gates are PASS with no unresolved critical blockers.

## Source Pointers

- Protocol/_docs_v1.0/reference/overview.md
- Protocol/_docs_v1.0/reference/architecture.md
- Protocol/_docs_v1.0/reference/protocol.md
- Protocol/_docs_v1.0/reference/users.md
- Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Protocol/_docs_v1.0/algorithms/03-up-routing.md
- Protocol/_docs_v1.0/algorithms/04-down-routing.md
- Protocol/_docs_v1.0/algorithms/05-identity-security.md
- Protocol/_docs_v1.0/mitigations/corner-cases.md
- Protocol/_docs_v1.0/mitigations/simulation-pipeline.md
- Implementation/Spec/03-system/05-hub-service-contracts.md
