# Gateway Failure Handling

## Failure classes

- UART transport interruption.
- ESP-NOW send/receive failures.
- Queue overflow.
- Invalid frame parse.
- State desynchronization after reboot.

## Required handling

- Fail closed on malformed frame paths.
- Surface errors to server observability channel.
- Resume forwarding safely after transient faults.
- Avoid stale forwarding loops via ttl/dedup invariants.

## OPEN DECISIONS

- Retry backoff schedule by failure class.
- Persistent vs volatile state policy across reboot.
