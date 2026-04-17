# Integration WP-03 Non-Functional Validation

## Goal

Validate non-functional behavior boundaries required for production readiness.

## Validation domains

- Reliability and recovery behavior.
- Queue and backpressure behavior.
- Authorization and audit behavior.
- Operational observability completeness.

## Tasks

T1. Reliability and recovery validation
- T1.1 Validate recovery from gateway disconnect.
- T1.2 Validate command path behavior under transient failures.

T2. Performance and pressure validation
- T2.1 Validate queue-pressure handling in server and gateway.
- T2.2 Validate telemetry burst behavior.

T3. Governance and security validation
- T3.1 Validate role matrix enforcement.
- T3.2 Validate audit event completeness.

T4. Observability validation
- T4.1 Validate required metrics availability.
- T4.2 Validate required structured logs availability.

## Deliverables

- NFR validation report.
- Action plan for unresolved NFR gaps.

## Acceptance criteria

- No unresolved critical NFR gap remains for launch phase entry.
