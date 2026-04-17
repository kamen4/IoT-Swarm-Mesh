# Gateway UART And Buffering

## UART frame contract

- Host-to-gateway and gateway-to-host frame envelope must include:
	- Frame length field.
	- Frame payload bytes.
	- Frame integrity field.
- Decision-closure document:
	- Implementation/Spec/05-gateway/uart-frame-decision-pack.md
- OPEN DECISION:
	- Length field encoding.
	- Integrity algorithm choice.
	- Resynchronization strategy after stream corruption.

## UART bridge requirements

- Frame boundary handling for host<->gateway channel.
- Input validation before forwarding into mesh.
- Output throttling when host or mesh side is saturated.

## Buffering model

- Separate queues for host-bound and mesh-bound traffic.
- Priority policy aligned to control/command/telemetry classes.
- Queue overflow handling with deterministic drop strategy.

## OPEN DECISIONS

- UART framing format details.
- Checksum policy on UART transport.
- Exact queue sizing by hardware profile.
