# Message Type Registry (Wave 1)

This file mirrors the registry in protocol reference and classifies direction and authentication expectations.

## Registry

| Message | Value | Direction | Auth |
| --- | --- | --- | --- |
| FIND | 0x01 | DOWN | TAG=0 |
| PONG | 0x02 | UP | TAG=0 |
| VERIFY | 0x10 | bidirectional | TAG=0 |
| PROTO | 0x11 | DOWN | HMAC |
| PROTO_R | 0x12 | UP | HMAC |
| START | 0x13 | DOWN | HMAC |
| ACK | 0x20 | UP | HMAC |
| PULL | 0x21 | UP | HMAC |
| PULL_R | 0x22 | DOWN | HMAC |
| IO_GET | 0x30 | DOWN | HMAC |
| IO_GET_R | 0x31 | UP | HMAC |
| IO_SET | 0x32 | DOWN | HMAC |
| IO_SET_R | 0x33 | UP | HMAC |
| IO_EVENT | 0x34 | UP | HMAC |
| HELLO | 0x40 | mesh-control (see algorithms) | TAG=0 or policy-defined |
| WAKE | 0x41 | mesh-control (see algorithms) | TAG=0 |
| BEACON | 0x42 | mesh-control (see algorithms) | TAG=0 |
| DECAY | 0x43 | mesh-control (see algorithms) | TAG=0 |
| FRAG | 0x7F | inherited | inherited |

## Wave 1 Constraint

- Registry values MUST remain unchanged for interoperability.
- Unknown msgType MUST be ignored safely and logged.
- Mesh-control direction semantics are defined in algorithm documents referenced by protocol section 6.8.

## Source Pointers

- Protocol/_docs_v1.0/reference/protocol.md
