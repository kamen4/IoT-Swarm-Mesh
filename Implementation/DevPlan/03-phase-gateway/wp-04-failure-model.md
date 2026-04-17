# Gateway WP-04 Failure Model And Recovery

## Goal

Define and validate gateway recovery behavior across expected fault classes.

## Tasks

T1. Fault catalog
- T1.1 Enumerate UART disconnect and corruption faults.
- T1.2 Enumerate mesh send/receive faults.
- T1.3 Enumerate state-reset faults after reboot.

T2. Fault response model
- T2.1 Define immediate containment action per fault class.
- T2.2 Define recovery transition path to active state.
- T2.3 Define escalation criteria to server/operator.

T3. Recovery verification obligations
- T3.1 Verify recovery from transient UART disconnect.
- T3.2 Verify queue behavior after recovery.
- T3.3 Verify routing safety invariants remain active during recovery.

## Deliverables

- Gateway fault handling runbook.
- Recovery verification report template.

## Acceptance criteria

- Each fault class has deterministic containment, recovery, and escalation behavior.
