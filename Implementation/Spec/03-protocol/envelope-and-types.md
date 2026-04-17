# Envelope And Message Types

## Envelope layout

- ROUTING_HEADER (mutable, hop-local): 12 bytes
- SECURE_HEADER (immutable for end-to-end auth): 18 bytes
- PAYLOAD
- TAG: 16 bytes (truncated HMAC for authenticated paths)

Total overhead: 46 bytes.

## ROUTING_HEADER fields

- ver (1)
- ttl (1)
- prevHopMac (6)
- charge (2)
- decayEpochHint (2)

## SECURE_HEADER fields

- dir (1)
- msgType (1)
- originMac (6)
- dstMac (6)
- msgId (2)
- seq (2)

## Message type registry (v1)

FIND, PONG, VERIFY, PROTO, PROTO_R, START,
ACK, PULL, PULL_R,
IO_GET, IO_GET_R, IO_SET, IO_SET_R, IO_EVENT,
HELLO, WAKE, BEACON, DECAY, FRAG.

## Endianness

- Multi-byte values use little-endian as documented.
