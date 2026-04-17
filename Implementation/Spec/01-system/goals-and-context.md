# System Goals And Context

## Goal

Provide a deployable on-premises IoT mesh control system based on the documented swarm protocol.

## Context boundary

- User-facing control path: Telegram -> Server -> Gateway -> Mesh -> Device.
- Telemetry return path: Device -> Mesh -> Gateway -> Server -> Storage/Monitoring.

## Core operating assumptions from docs

- Mesh transport uses ESP-NOW.
- Gateway is the bridge between host and mesh.
- Host side is containerized architecture with a business server as source of truth.

## Non-goals in this specification

- Not redefining protocol mathematics.
- Not redefining simulation scoring models.
- Not adding cloud dependencies.
