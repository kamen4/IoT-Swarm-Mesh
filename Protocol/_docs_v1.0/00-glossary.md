# Glossary

## General Terms

**ESP-NOW**  
A connectionless MAC protocol for ESP32/ESP8266 providing direct device-to-device communication on 2.4 GHz without requiring an access point.

**HUB (Host)**  
Central server (PC/Raspberry Pi/Laptop) running Docker containers: business logic, database, Telegram bot, UART interface. The single point of truth for device configuration and authorization.

**Gateway Device**  
An ESP device physically connected to the HUB via UART; acts as the mesh root for DOWN traffic and a normal node for UP traffic.

**Mesh**  
Network of ESP devices communicating via ESP-NOW with forwarding (routing) across multiple hops.

**Neighbor**  
A directly reachable peer within single-hop ESP-NOW range.

**Frame / Packet**  
A single ESP-NOW transmission (bounded by the ESP-NOW maximum) containing protocol headers, payload, and HMAC tag.

## Security & Authentication

**SPAKE2**  
Symmetric Password-Authenticated Key Exchange (version 2). A cryptographic protocol used during device onboarding to derive a shared secret (`S_PASSWORD`) from the device's `CONNECTION_KEY` without transmitting the key itself.

**S_PASSWORD**  
Per-device secret derived during SPAKE2. The only cryptographic key used by the protocol after onboarding; used for end-to-end HMAC authentication of all device-to-server messages and vice versa.

**HMAC**  
Hash-based Message Authentication Code; provides authenticity and integrity verification of a message. Uses `HMAC-SHA256` truncated to 16 bytes.

**CONNECTION_STRING**  
User-visible credential during onboarding: device MAC address + base64(SHA256(CONNECTION_KEY)). Scanned from device QR code and sent to Telegram bot to initiate registration.

**CONNECTION_KEY**  
Random per-device secret baked into the device's QR code. Hash of this is in `CONNECTION_STRING`; used as input to SPAKE2.

## Routing & Topology

**UP Direction**  
Traffic flowing from a device toward the gateway (e.g., telemetry, device->server commands).

**DOWN Direction**  
Traffic flowing from the gateway to devices (e.g., server commands, updates).

**Charge (q_up, q_total)**  
Numeric metric advertised by a node indicating its "connectivity quality":

- `q_up`: charge for routing UP (higher = better path to gateway)
- `q_total`: total/centrality charge for general traffic (often useful for disseminating gateway-originated broadcasts/control)
  - Unicast DOWN delivery uses a charge-induced single-parent tree with a forwarding threshold `q_forward` (see `Tree Broadcast`).

Neighbors inspect incoming packets' `ROUTING_HEADER.charge` field to build a local neighbor-charge map.

**Top 50% Forwarding**  
UP routing strategy: forward to the neighbor with highest `q_up`, plus top 50% of remaining neighbors (minimum 1). Creates multi-path resilience while limiting replication.

**Decay Epoch**  
Network-wide synchronization mechanism preventing unbounded charge growth. Triggered by a `DECAY` mesh-control message; nodes reset or dampen their charge metrics and bump `lastDecayEpoch`.

**BEACON**  
Periodic link-layer broadcast from gateway (destination `FF:FF:FF:FF:FF:FF`) carrying an implementation-defined convergence hint for charge-based DOWN tree formation.

`BEACON` is mesh-control and is not end-to-end authenticated (`TAG = 0`); receivers MUST treat it as an untrusted hint.

**Forward-Eligibility Threshold (q_forward)**  
Minimum charge threshold used by DOWN tree-broadcast:

- A node forwards DOWN only to children whose observed `q_total` is at least `q_forward`.
- Nodes below `q_forward` are treated as “cold” / not-yet-trained and should rely on WAKE + PULL delivery.

**Parent / Child**  
In the DOWN tree:

- **Parent**: the neighbor a node selects as the link back toward gateway (higher `q_total` + best RSSI tie-break; in the ideal model the parent must have strictly higher charge than the child)
- **Child**: a neighbor that has selected the current node as parent. Tracked in `children[]` table.

**Tree Broadcast**  
For DOWN, a node forwards only to its direct children that satisfy `q_total(child) >= q_forward` and never back to its parent; structurally prevents loops.

## Message & Frame Structure

**ROUTING_HEADER**  
Mutable per-hop metadata: `ver`, `ttl`, `prevHopMac`, `charge`, `decayEpochHint`. Not included in end-to-end HMAC.

**SECURE_HEADER**  
Immutable fields preserved across hops: `dir`, `msgType`, `originMac`, `dstMac`, `msgId`, `seq`. Included in end-to-end HMAC.

**PAYLOAD**  
Application-level data (command, telemetry, etc.). Included in end-to-end HMAC.

**TAG**  
HMAC authentication tag (16 bytes). Computed as `HMAC(S_PASSWORD, SECURE_HEADER | PAYLOAD)` for end-to-end messages.

**msgId**  
Unique message identifier per origin device, used for deduplication. Combined with `originMac` to form the dedup cache key `(originMac, msgId)`.

**seq**  
Per-endpoint session sequence number for replay detection. Incremented per end-to-end command or telemetry; out-of-order or repeated `seq` values trigger drop.

**dstMac**  
Destination MAC address or broadcast address (`FF:FF:FF:FF:FF:FF` for mesh-control broadcast).

**ttl (Time-To-Live)**  
Hop limit: decremented on forward, packet dropped if reaches 0.

## Device States & Lifecycle

**Pending**  
Device registered in HUB but not yet found on mesh. Awaiting `FIND` -> `PONG` sequence.

**Verified**  
Device found and SPAKE2 completed; `S_PASSWORD` established. Awaiting interaction protocol fetch.

**Connected**  
Device fully onboarded; interaction protocol obtained; ready for commands.

## User Roles

**User**  
Can send requests to devices and read device information. No admin capabilities.

**Dedicated Admin**  
Can perform all User actions plus CRUD operations on devices and users.

**Admin**  
Can perform all Dedicated Admin actions plus grant/revoke Dedicated Admin roles to other users.

## Protocol Message Types

**FIND**  
Broadcast to locate a device by MAC during onboarding.

**PONG**  
Reply to FIND confirming device presence.

**VERIFY**  
SPAKE2 handshake message used during onboarding to derive `S_PASSWORD` (uses a step byte in payload).

**PROTO**  
Request for device's interaction protocol (list of elements, types, formats).

**PROTO_R**  
Device response containing its interaction protocol specification.

**START**  
Server signals device is fully onboarded and ready for normal operation.

**FRAG**
Fragmentation wrapper message used when a logical payload does not fit into a single ESP-NOW frame.

**IO_GET / IO_GET_R**
Interaction read: server requests an element value; device returns the value.

**IO_SET / IO_SET_R**
Interaction write: server sets an input element value; device returns status (and optionally echoes the applied value).

**IO_EVENT**
Device-originated element update/event (telemetry).

**WAKE**  
Link-layer broadcast from a device waking from deep sleep to re-announce presence and reattach to tree.

**HELLO**  
Keep-alive / presence announcement from a device to neighbors (carries parent selection and status).

**ACK**  
End-to-end acknowledgement: device confirms receipt of a DOWN command back to server.

**DECAY**  
Mesh-control broadcast triggering network-wide charge reset/dampening.

**PULL / PULL_R**  
Command polling mechanism for sleepy devices: device requests pending commands; server replies with queue.

## Performance & Constraints

**ESP_NOW_MAX**  
Maximum frame size for ESP-NOW.

**PAYLOAD_MAX**  
Available bytes for application payload after protocol overhead (headers + TAG).  
Formula: `PAYLOAD_MAX = ESP_NOW_MAX - 46` bytes (overhead = 12 + 18 + 16).

**OVERHEAD_LEN**  
Total bytes consumed by `ROUTING_HEADER`, `SECURE_HEADER`, and `TAG`: 46 bytes.

**Neighbor Capacity**  
Practical limit on remembered neighbors per device due to RAM constraints on ESP devices.

**Dedup Cache**  
Fixed-size LRU cache of recent `(originMac, msgId)` pairs, kept for a short TTL.

**Forward Queue**  
Bounded queue for outgoing messages. Uses priority drop (control > command > telemetry).

---

## See Also

- [Onboarding Algorithm](algorithms/01-onboarding.md)
- [Message Envelope Format](algorithms/02-message-envelope.md)
- [UP Routing](algorithms/03-up-routing.md)
- [DOWN Routing](algorithms/04-down-routing.md)
- [Identity & Security Details](algorithms/05-identity-security.md)
