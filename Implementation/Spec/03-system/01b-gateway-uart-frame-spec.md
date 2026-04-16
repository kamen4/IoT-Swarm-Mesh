# Gateway UART Frame Specification (Wave 1)

## Objective

Define a deterministic UART framing contract between Gateway and HUB.

## Framing Contract

- Requirement: UART transport MUST use byte-framed packets with explicit start marker, length, and checksum.
  - Source: Protocol/_docs_v1.0/reference/architecture.md
- Requirement: Each frame MUST use this binary layout:
  - SOF: u8 (0x7E)
  - VER: u8 (0x01)
  - TYPE: u8 (0x01 mesh-up, 0x02 mesh-down, 0x03 control)
  - LEN: u16 little-endian (payload byte length only)
  - PAYLOAD: LEN bytes
  - CRC16: u16 little-endian over VER|TYPE|LEN|PAYLOAD
  - Source: Protocol/_docs_v1.0/mitigations/corner-cases.md
- Requirement: If SOF (0x7E) or ESC (0x7D) appears in VER|TYPE|LEN|PAYLOAD|CRC16, sender MUST escape byte as ESC followed by byte XOR 0x20.
  - Source: Protocol/_docs_v1.0/mitigations/corner-cases.md

## Checksum Profile

- Requirement: CRC16 MUST use CRC-16/CCITT-FALSE with polynomial 0x1021, init 0xFFFF, no xorout.
  - Source: Protocol/_docs_v1.0/mitigations/corner-cases.md
- Requirement: Receiver MUST drop frame on CRC mismatch and increment malformed-frame counter.
  - Source: Protocol/_docs_v1.0/mitigations/corner-cases.md

## Recovery and Backpressure

- Requirement: Receiver MUST re-synchronize by scanning for next SOF on parse failure.
  - Source: Protocol/_docs_v1.0/mitigations/corner-cases.md
- Requirement: Receiver MUST enforce per-frame read timeout of 500 ms after SOF; timeout MUST drop partial frame.
  - Source: Protocol/_docs_v1.0/mitigations/corner-cases.md
- Requirement: Bridge MUST expose queue occupancy as backpressure signal to HUB control path.
  - Source: Protocol/_docs_v1.0/mitigations/corner-cases.md

## Metadata Preservation

- Requirement: PAYLOAD for TYPE 0x01 and 0x02 MUST carry complete protocol envelope without field mutation.
  - Source: Protocol/_docs_v1.0/reference/protocol.md
- Requirement: Bridge MUST preserve originMac, dstMac, msgId, seq, ttl, prevHopMac, and charge fields end-to-end across UART hop.
  - Source: Protocol/_docs_v1.0/algorithms/02-message-envelope.md

## Source Pointers

- Protocol/_docs_v1.0/reference/architecture.md
- Protocol/_docs_v1.0/mitigations/corner-cases.md
- Protocol/_docs_v1.0/reference/protocol.md
- Protocol/_docs_v1.0/algorithms/02-message-envelope.md
