# Device Library WP-04 Routing And Sleepy Device Behavior

## Goal

Implement endpoint-side routing participation and sleepy-device command retrieval behavior.

## Tasks

T1. UP routing participation
- T1.1 Define best-neighbor forwarding behavior.
- T1.2 Define dedup and ttl handling obligations.

T2. DOWN routing participation
- T2.1 Define parent relation usage boundary.
- T2.2 Define children-forwarding obligations where applicable.

T3. Sleepy-device behavior
- T3.1 Define wake signaling behavior.
- T3.2 Define pull retrieval lifecycle behavior.
- T3.3 Define pending command ordering and expiration behavior.

T4. Resource safety
- T4.1 Define bounded memory obligations for neighbor and dedup views.
- T4.2 Define queue-pressure mitigation behavior.

## Deliverables

- Routing participation specification.
- Sleepy-device behavior specification.

## Acceptance criteria

- Library behavior remains protocol-conformant under constrained resource operation.
