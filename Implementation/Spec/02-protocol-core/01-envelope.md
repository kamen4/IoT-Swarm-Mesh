# Envelope Specification

## Requirement Summary

- Every protocol frame MUST follow:
  - ROUTING_HEADER (mutable)
  - SECURE_HEADER (authenticated)
  - PAYLOAD
  - TAG (HMAC truncation or zero for allowed non-E2E types)

## Header Fields

### ROUTING_HEADER

- ver
- ttl
- prevHopMac
- charge
- decayEpochHint

### SECURE_HEADER

- dir
- msgType
- originMac
- dstMac
- msgId
- seq

## Authentication Boundary

- ROUTING_HEADER is NOT included in end-to-end HMAC.
- SECURE_HEADER and PAYLOAD are included in HMAC calculation.

## Byte Constraints

- Protocol overhead is fixed at 46 bytes.
- PAYLOAD_MAX MUST be computed as ESP_NOW_MAX minus 46.

## Replay and Dedup

- seq is used for replay protection for authenticated flows.
- Dedup key is (originMac, msgId) with bounded cache.

## Source Pointers

- Protocol/_docs_v1.0/algorithms/02-message-envelope.md
- Protocol/_docs_v1.0/reference/protocol.md
- Protocol/_docs_v1.0/reference/byte-sizes.md
