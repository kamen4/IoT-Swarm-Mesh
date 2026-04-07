# Byte sizes

## Byte sizes

This section defines the binary sizes of each field so the remaining bytes for `PAYLOAD` can be calculated.

`ESP_NOW_MAX` is the maximum number of bytes available for this protocol message in a single ESP-NOW frame. Treat it as a platform/config parameter.

Fixed sizes:

**ROUTING_HEADER**:

- `ver`: 1 byte (`uint8`)
- `ttl`: 1 byte (`uint8`)
- `prevHopMac`: 6 bytes
- `charge`: 2 bytes (`uint16`, normalized charge)
- `decayEpochHint`: 2 bytes (`uint16`)

Total `ROUTING_HEADER_LEN = 12` bytes.

**SECURE_HEADER**:

- `dir`: 1 byte (`uint8`)
- `msgType`: 1 byte (`uint8`)
- `originMac`: 6 bytes
- `dstMac`: 6 bytes
- `msgId`: 2 bytes (`uint16`)
- `seq`: 2 bytes (`uint16`)

Total `SECURE_HEADER_LEN = 18` bytes.

**TAG**:

- `TAG_LEN`: 16 bytes (HMAC-SHA256 truncated to 16 bytes)

Total envelope overhead:

`OVERHEAD_LEN = ROUTING_HEADER_LEN + SECURE_HEADER_LEN + TAG_LEN = 46` bytes.

Available payload per single ESP-NOW frame:

`PAYLOAD_MAX = ESP_NOW_MAX - OVERHEAD_LEN`

Example (if `ESP_NOW_MAX = 250`): `PAYLOAD_MAX = 204` bytes.
