# Device Library Routing And IO

## Routing responsibilities

- Implement UP forwarding path behavior.
- Implement DOWN acceptance and children-forwarding behavior where applicable.
- Maintain local neighbor and dedup views sufficient for protocol conformance.

## Interaction protocol responsibilities

- Expose element descriptors through PROTO_R.
- Handle IO_GET/IO_SET requests and produce IO_GET_R/IO_SET_R.
- Emit IO_EVENT telemetry/events with documented semantics.

## Application callback contract

- app_on_io_get:
	- Receives element identifier and read context.
	- Returns current value or read error status.
- app_on_io_set:
	- Receives element identifier, desired value, and write context.
	- Returns apply status for IO_SET_R generation.
- app_on_forward_queue_pressure:
	- Receives queue pressure signal to allow application-level load shedding.

## OPEN DECISIONS

- Application-level retry semantics per element type.
- Local caching policy for frequently queried values.
