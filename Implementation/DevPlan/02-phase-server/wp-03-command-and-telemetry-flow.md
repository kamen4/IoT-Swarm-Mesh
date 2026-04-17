# Server WP-03 Command And Telemetry Flow

## Goal

Implement command dispatch and telemetry ingestion flows aligned with protocol message model.

## Tasks

T1. Command intake
- T1.1 Define command request validation from user interface.
- T1.2 Define device capability check against stored interaction model.

T2. Command dispatch
- T2.1 Define IO_SET and IO_GET dispatch lifecycle.
- T2.2 Define ACK and response correlation handling.
- T2.3 Define retry and timeout policy for command outcomes.

T3. Sleepy-device command path
- T3.1 Define pending command queue model.
- T3.2 Define PULL and PULL_R response policy.
- T3.3 Define command expiration and stale cancellation rules.
- T3.4 Define conflict handling when multiple pending commands target same element.
- T3.5 Define ordering guarantees for mixed command priorities.

T4. Telemetry flow
- T4.1 Define IO_EVENT ingest contract.
- T4.2 Define normalization and storage pipeline.
- T4.3 Define backpressure behavior for burst telemetry.

## Deliverables

- Command lifecycle specification.
- Telemetry ingestion specification.

## Acceptance criteria

- Command and telemetry flows are complete, observable, and failure-aware.
