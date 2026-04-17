# Gateway WP-02 UART Bridge

## Goal

Implement robust frame bridging between server channel and mesh channel.

## Tasks

T1. Frame contract implementation
- T1.1 Implement frame boundary handling.
- T1.2 Implement frame integrity validation path.
- T1.3 Implement stream resynchronization path.

T2. Bridge pipelines
- T2.1 Define host->mesh ingest and validation sequence.
- T2.2 Define mesh->host ingest and validation sequence.

T3. Backpressure and flow control
- T3.1 Define queue pressure thresholds.
- T3.2 Define throttling behavior when either side saturates.
- T3.3 Define deterministic drop policy.

## Deliverables

- UART bridge behavior specification.
- Flow-control and drop-policy specification.

## Acceptance criteria

- Bridge preserves frame integrity and degrades predictably under pressure.
