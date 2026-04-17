# Gateway WP-01 Runtime Foundation

## Goal

Define gateway runtime state model and resource boundaries.

## Tasks

T1. Runtime state model
- T1.1 Define startup state, active state, degraded state, and recovery state.
- T1.2 Define state transition triggers and guard conditions.

T2. Resource boundaries
- T2.1 Define neighbor table bounds.
- T2.2 Define dedup cache bounds.
- T2.3 Define queue bounds for host-bound and mesh-bound traffic.

T3. Control-plane behavior
- T3.1 Define control message participation boundaries.
- T3.2 Define safe handling for untrusted hint messages.

## Deliverables

- Runtime state model specification.
- Resource boundary specification.

## Acceptance criteria

- Runtime state transitions are deterministic and bounded by resource limits.
