# Gateway WP-03 Routing Execution

## Goal

Implement gateway-side routing execution responsibilities aligned with protocol contracts.

## Tasks

T1. UP path handling
- T1.1 Define UP reception and host delivery path.
- T1.2 Define dedup and ttl enforcement for UP traffic.

T2. DOWN path initiation
- T2.1 Define DOWN emission policy from host commands.
- T2.2 Define children-only propagation rule enforcement.

T3. Control message handling
- T3.1 Define BEACON handling and emission policy.
- T3.2 Define DECAY handling policy.
- T3.3 Define WAKE related handling boundaries.

T4. Routing observability
- T4.1 Define counters for forwarded, dropped, deduplicated, and ttl-expired traffic.
- T4.2 Define event signals for routing anomalies.

## Deliverables

- Gateway routing execution specification.
- Routing observability metrics package.

## Acceptance criteria

- Gateway routing behavior is consistent with documented UP and DOWN constraints.
