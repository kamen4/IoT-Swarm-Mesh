# WP-03 Architecture Baseline

## Goal

Freeze architecture interpretation boundaries before implementation starts.

## Tasks

T1. Host architecture baseline
- T1.1 Confirm required host components from source architecture docs.
- T1.2 Define host component responsibility boundaries.

T2. Gateway architecture baseline
- T2.1 Confirm gateway bridge and forwarding responsibilities.
- T2.2 Define gateway runtime state boundaries.

T3. Device library baseline
- T3.1 Confirm required library modules from protocol flows.
- T3.2 Define endpoint-library integration boundary.

T4. Interface baseline
- T4.1 Define server-gateway channel contract fields.
- T4.2 Define server-device logical contract through protocol messages.
- T4.3 Define telemetry flow boundary.

## Deliverables

- Architecture baseline package.
- Interface boundary summary.

## Acceptance criteria

- No cross-component ownership ambiguity remains.
