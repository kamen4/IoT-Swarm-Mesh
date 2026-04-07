# Message security & envelope

## Message security

This protocol has 2 logical layers:

1. **End-to-end security** between HUB (server side) and a specific device.
2. **Mesh routing header** used for forwarding inside ESP-NOW mesh.

Because routing metadata (like `TTL`, `prevHop`, `charge`) is modified by intermediate nodes, end-to-end HMAC must cover only the immutable part of the message.

### Keys

- `S_PASSWORD` - per-device secret generated during SPAKE2 (device <-> server). Used for end-to-end HMAC of device commands and telemetry.

## Message envelope

Every ESP-NOW frame carries a single protocol message with the following logical structure:

```txt
ROUTING_HEADER | SECURE_HEADER | PAYLOAD | TAG
```

**ROUTING_HEADER** (mutable, can change on every hop; NOT included into end-to-end HMAC):

- `ver` - protocol version
- `ttl` - hop limit (decremented on each forward)
- `prevHopMac` - MAC of the sender of this hop
- `charge` - routing metric value advertised by `prevHopMac` (see Swarm routing)
- `decayEpochHint` - last decay epoch known by sender (helps convergence)

**SECURE_HEADER** (immutable during forwarding; included into end-to-end HMAC):

- `dir` - `UP` (to gateway) or `DOWN` (from gateway)
- `msgType` - message type
- `originMac` - MAC of the original sender (stays constant across hops)
- `dstMac` - target MAC, or broadcast address for mesh-control.
  - For link-layer broadcast control messages, use `dstMac = FF:FF:FF:FF:FF:FF`.
- `msgId` - message id unique per `originMac` (used for dedup/loop prevention)
- `seq` - per-session sequence number for end-to-end anti-replay

**PAYLOAD** - message payload bytes (application-level or mesh-control-level).

**TAG** - authentication tag:

- For end-to-end messages: `TAG = HMAC(S_PASSWORD, SECURE_HEADER | PAYLOAD)`

Mesh-control message authentication is not specified in this document.

Intermediate nodes MUST NOT modify `SECURE_HEADER` or `PAYLOAD`.

## Replay protection and deduplication

- Endpoints (server/device) MUST validate `seq` to reject replays for end-to-end messages.
- All nodes (including intermediate forwarders) MUST keep a fixed-size cache of the last `N` seen `(originMac, msgId)` pairs.
  - If an incoming message is already in cache, it MUST be dropped.
  - Cache is size-based (last `N` entries), not time-based.

This cache is required to prevent loops and storms in swarm routing.

## Byte sizes

See [\_docs/reference/byte-sizes.md](../reference/byte-sizes.md).
