# Protocol Specification (Wire Format)

This document is the **normative wire-format specification** for the ESP‑NOW mesh protocol.

It defines:

- Binary encoding rules (endianness, strings)
- Frame envelope format and how `TAG` is interpreted
- `msgType` registry and payload formats
- Fragmentation (`FRAG`) for payloads larger than a single ESP‑NOW frame

For routing behaviour, see:

- [UP Routing — Swarm (charge-based)](../algorithms/03-up-routing.md)
- [DOWN Delivery — Tree-First Broadcast Concept](../algorithms/04-down-routing.md)

---

## 1) Encoding conventions

- **Byte order**: all multi-byte integers are **little-endian**.
- **MAC addresses**: 6 bytes in the same order as shown by the ESP stack.
- **Strings**: `u8 len` followed by `len` bytes of UTF‑8. Maximum 255 bytes per string.
- **Booleans**: `u8` (`0` = false, non‑zero = true).

---

## 2) Envelope (single ESP‑NOW frame)

Every ESP‑NOW frame carries exactly one protocol envelope:

`ROUTING_HEADER | SECURE_HEADER | PAYLOAD | TAG`

Byte sizes are defined in [Byte sizes](byte-sizes.md).

### 2.1 `TAG` interpretation

`TAG` is always 16 bytes. Its meaning depends on the message authentication mode:

- **End-to-end (E2E) authenticated** messages (server ↔ device after SPAKE2):
  - `TAG = Trunc16(HMAC_SHA256(S_PASSWORD, SECURE_HEADER | PAYLOAD))`
- **Non‑E2E** messages (pre‑SPAKE2 onboarding discovery; some mesh-control messages):
  - `TAG` MUST be all‑zero bytes and MUST be ignored by receivers.

Notes:

- Intermediate forwarders do not have `S_PASSWORD` and therefore MUST NOT attempt to validate `TAG`.
- Security requirements for mesh‑control messages are defined in [Identity & Security](../algorithms/05-identity-security.md).

---

## 3) Direction and broadcast

- `SECURE_HEADER.dir`:
  - `UP` — toward the gateway (device → server)
  - `DOWN` — from the gateway (server → device)

- Link-layer broadcast is represented by:
  - `SECURE_HEADER.dstMac = FF:FF:FF:FF:FF:FF`

---

## 4) Fragmentation (`FRAG`)

Some payloads (e.g., large interaction protocols, batched command queues) can exceed `PAYLOAD_MAX`.

In this case, the sender MUST transmit the content as a sequence of `FRAG` messages.

### 4.1 `FRAG` message type

- `msgType = FRAG`
- Each fragment is a normal envelope with its own `msgId` (so hop-by-hop dedup works unchanged).
- The fragment payload is:

```с
struct FragPayload {
  u16 fragGroupId;     // groups fragments of the same logical message
  u8  fragIndex;       // 0..fragCount-1
  u8  fragCount;       // total number of fragments (>= 1)
  u16 totalLen;        // total bytes after reassembly (innerPayload length)
  u8  innerMsgType;    // the logical message type being transported
  u8  innerPayload[];  // slice of the logical payload
}
```

- All fragments in a group MUST share the same:
  - `originMac`, `dstMac`, `dir`, `seq`, `fragGroupId`, `fragCount`, `totalLen`, `innerMsgType`.

### 4.2 Reassembly rules (endpoints)

Endpoints (server/device) reassemble fragments keyed by `(originMac, fragGroupId)`:

- If a fragment with the same `(originMac, fragGroupId, fragIndex)` is received twice, the duplicate is ignored.
- If reassembly does not complete within a bounded time window, the partial group is dropped.
- After all fragments are received, the endpoint reconstructs `innerPayload` by concatenating fragments in index order and dispatches it as if it were the payload of `innerMsgType`.

---

## 5) `msgType` registry (v1)

`SECURE_HEADER.msgType` is `u8`. The following values are reserved for v1:

| Name       | Value |
| ---------- | ----- |
| `FIND`     | 0x01  |
| `PONG`     | 0x02  |
| `VERIFY`   | 0x10  |
| `PROTO`    | 0x11  |
| `PROTO_R`  | 0x12  |
| `START`    | 0x13  |
| `ACK`      | 0x20  |
| `PULL`     | 0x21  |
| `PULL_R`   | 0x22  |
| `IO_GET`   | 0x30  |
| `IO_GET_R` | 0x31  |
| `IO_SET`   | 0x32  |
| `IO_SET_R` | 0x33  |
| `IO_EVENT` | 0x34  |
| `HELLO`    | 0x40  |
| `WAKE`     | 0x41  |
| `BEACON`   | 0x42  |
| `DECAY`    | 0x43  |
| `FRAG`     | 0x7F  |

---

## 6) Payload formats (v1)

Unless specified otherwise, integer fields below follow the encoding conventions in section 1.

### 6.1 Onboarding discovery

#### `FIND` (server → mesh)

- `dir = DOWN`
- `dstMac = FF:FF:FF:FF:FF:FF`
- Auth: non‑E2E (`TAG = 0`)

Payload:

```с
struct FindPayload {
  u8  targetMac[6];
}
```

#### `PONG` (device → gateway)

- `dir = UP`
- `dstMac = gatewayMac` (learned as `originMac` from the received `FIND`)
- Auth: non‑E2E (`TAG = 0`)

Payload:

```с
struct PongPayload {
  u16 echoFindMsgId;   // msgId from the FIND being answered
}
```

### 6.2 SPAKE2 handshake (`VERIFY`)

All `VERIFY` messages use non‑E2E authentication (`TAG = 0`).

`VERIFY` payload begins with a step byte:

```с
struct VerifyPayload {
  u8 step;             // 1..4
  u8 data[];           // step-dependent
}
```

Steps:

- Step 1 (server → device): `step = 1`, empty `data`.
- Step 2 (device → server): `step = 2`, `data = T_d` (encoded SPAKE2 group element).
- Step 3 (server → device): `step = 3`, `data = T_s | V_s`.
- Step 4 (device → server): `step = 4`, `data = V_d`.

Lengths:

- `T_d`, `T_s` are encoded group elements as defined by the crypto profile in [Identity & Security](../algorithms/05-identity-security.md).
- `V_s`, `V_d` are 16-byte values: `Trunc16(HMAC_SHA256(S_PASSWORD, "SERVER_OK"))` and `Trunc16(HMAC_SHA256(S_PASSWORD, "DEVICE_OK"))`.

### 6.3 Interaction protocol exchange

#### `PROTO` (server → device)

- `dir = DOWN`
- Auth: E2E HMAC (`TAG = HMAC`)

Payload: empty.

#### `PROTO_R` (device → server)

- `dir = UP`
- Auth: E2E HMAC (`TAG = HMAC`)
- If the payload does not fit a single frame, send it via `FRAG(innerMsgType=PROTO_R)`.

Payload (v1):

```с
struct ProtoRPayload {
  u8  protoVersion;    // = 1
  u8  elementCount;
  Element elements[elementCount];
}

struct Element {
  u16 id;
  u8  ioType;          // 0=O (output), 1=I (input)
  u8  format;          // see ValueFormat
  u8  nameLen;
  u8  name[nameLen];

  // If format == ENUM:
  u8  enumCount;
  EnumValue enumValues[enumCount];
}

struct EnumValue {
  u8 valueLen;
  u8 value[valueLen];
}

enum ValueFormat : u8 {
  BOOL   = 1,
  INT32  = 2,
  FLOAT32= 3,
  ENUM   = 4,
  STRING = 5,
}
```

### 6.4 Start / connect

#### `START` (server → device)

- `dir = DOWN`
- Auth: E2E HMAC (`TAG = HMAC`)

Payload (v1): empty.

Note: after SPAKE2, this protocol does not provision additional identity material or shared network keys; endpoint authentication relies on `S_PASSWORD` (see [Identity & Security](../algorithms/05-identity-security.md)).

### 6.5 Application I/O (interaction)

These messages operate on elements defined by `PROTO_R`.

Common:

- `elementId` is `u16`.
- `status` is `u8`:
  - `0` = OK
  - non‑zero = error (implementation-defined)

Value encoding depends on the element `format`:

- `BOOL`: `u8`
- `INT32`: `i32`
- `FLOAT32`: `f32` (IEEE-754)
- `ENUM`: `u8` (index into `enumValues[]`)
- `STRING`: `u8 len` + UTF‑8 bytes

#### `IO_GET` / `IO_GET_R`

`IO_GET` payload:

```с
struct IoGetPayload {
  u16 elementId;
}
```

`IO_GET_R` payload:

```с
struct IoGetRPayload {
  u16 elementId;
  u8  status;
  u8  value[];   // omitted if status != 0
}
```

#### `IO_SET` / `IO_SET_R`

`IO_SET` payload:

```с
struct IoSetPayload {
  u16 elementId;
  u8  value[];
}
```

`IO_SET_R` payload:

```с
struct IoSetRPayload {
  u16 elementId;
  u8  status;
  u8  value[];   // optional echo of the applied value
}
```

#### `IO_EVENT`

`IO_EVENT` payload:

```с
struct IoEventPayload {
  u16 elementId;
  u8  value[];
}
```

### 6.6 ACK

`ACK` is an end-to-end authenticated message (E2E HMAC).

Payload:

```с
struct AckPayload {
  u16 ackedMsgId;   // msgId of the DOWN message being acknowledged
}
```

### 6.7 Sleepy device polling (`PULL` / `PULL_R`)

`PULL` / `PULL_R` are end-to-end authenticated (E2E HMAC).

`PULL` payload:

```с
struct PullPayload {
  u8  maxCount;    // requested number of queued items
  u16 maxBytes;    // requested total payload budget
}
```

`PULL_R` payload:

```с
struct PullRPayload {
  u8  count;
  Item items[count];
}

struct Item {
  u8  innerMsgType;
  u16 innerLen;
  u8  innerPayload[innerLen];
}
```

If `PULL_R` does not fit, send it via `FRAG(innerMsgType=PULL_R)`.

### 6.8 Mesh-control messages (`HELLO`, `WAKE`, `BEACON`, `DECAY`)

Mesh-control payloads and their authentication requirements are defined in:

- [DOWN Delivery — Tree-First Broadcast Concept](../algorithms/04-down-routing.md)
- [Identity & Security](../algorithms/05-identity-security.md)

At minimum:

- `BEACON` payload contains an implementation-defined convergence hint for charge-based DOWN tree formation (e.g., a `q_total` / `q_forward` hint).
- `DECAY` payload contains a monotonic `decayEpoch` and a decay `percent`.
