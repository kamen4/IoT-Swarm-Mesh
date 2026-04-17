# Server Integration Specification

## Telegram integration

- Accept command intents and onboarding inputs.
- Route outputs and alerts back to authorized users.

## Gateway integration

- Use UART bridge channel for all mesh-bound protocol traffic.
- Implement retransmission and timeout policy for command flows.

## Queue and service integration

- Use internal queueing between ingress, routing, and persistence stages.
- OPEN DECISION: exact queue implementation details if not locked.

## Internal queue contract

- Ingress queue:
	- Accepts validated command intents from user control interface.
- Dispatch queue:
	- Carries protocol-ready messages toward gateway bridge.
- Acknowledgement queue:
	- Carries ACK and completion status back to command lifecycle manager.
- Telemetry queue:
	- Carries inbound IO_EVENT and related telemetry payloads to storage pipeline.

## Monitoring integration

- Expose metrics for command latency, delivery status, onboarding success, and queue pressure.
