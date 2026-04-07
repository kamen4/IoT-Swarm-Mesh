# Corner Cases and Mitigations (ESP‑NOW Mesh)

This document lists failure modes for the current protocol and practical mitigations suitable for constrained ESP devices.

Scope:

- Protocol definition: `Protocol.md`
- DOWN strategy: `DOWN.md`

Assumptions:

- Neighbor/peer capacity is limited (order of ~20).
- `ESP_NOW_MAX` depends on ESP-NOW stack/config (often ~250 bytes).
- Some devices are deep-sleep (TX-only most of the time).
- UP is multi-path/swarm; DOWN is tree-first broadcast (children-only forwarding).

---

## 1) Capacity Limits (Neighbors / Peers)

### 1.1 Dense clique (21+ devices all in range)

Failure:

- A node cannot store all visible neighbors; direct unicast-to-neighbor fails for evicted MACs.

Mitigations:

- LRU eviction + sticky set: keep `Gateway`, `parent`, and 2–3 stable children.
- Two-tier tables: `stable[]` + `transient[]` (probe new neighbors via transient).
- On-demand peer slot (if ESP‑NOW stack allows): temporarily add dst peer → send → remove.
- Link-layer broadcast with `ttl=0` / “no-forward” for clique-only delivery (dst filters by `dstMac`).

### 1.2 New node cannot enter tables

Failure:

- All neighbors are “full”; a joiner stays invisible.

Mitigations:

- Utility-based eviction (not only lastSeen): include RSSI + successRate + role (parent/child).
- Periodic probe window: free 1 transient slot every `T` seconds to sample new MACs.

---

## 2) Tree Formation and Maintenance (BEACON Gradient)

### 2.1 Cold start (all metrics are zero)

Failure:

- Without a root signal, convergence is slow and unstable.

Mitigations:

- Gateway `BEACON` (broadcast to `FF:FF:FF:FF:FF:FF`) with a gradient value (`g=0` at root).
- Parent selection by best gradient + RSSI penalty; compute `g_self = min(g_parent + 1)`.
- Forward jitter (random 10–100ms) to reduce synchronized collisions.

### 2.2 Parent flapping

Failure:

- Devices oscillate between parents; DOWN becomes unreliable.

Mitigations:

- Hysteresis: switch parent only if improvement is significant (e.g., ≥2 hops or strong RSSI delta).
- Parent dead timer: expire parent if not heard for `T_dead`.
- Rate-limit re-parenting (cooldown window).

### 2.3 Child table overflow

Failure:

- Parent cannot remember all children; DOWN subtree coverage degrades.

Mitigations:

- Child table LRU; prioritize children that recently produced traffic/ACKs.
- If child table is full, allow the parent to implicitly “drop” stale children; the child will re-attach via WAKE/HELLO.

---

## 3) Sleepy Nodes (Deep Sleep)

### 3.1 DOWN to a sleeping device

Failure:

- A deep-sleep device cannot receive; retries create storms.

Mitigations:

- Treat deep-sleep devices as **PULL-only**: queue commands on HUB; deliver after WAKE.
- Gateway retries only when device is recently seen (WAKE/HELLO within a freshness window).

### 3.2 Wake with stale topology

Failure:

- Device wakes and tries to use dead/stale neighbors.

Mitigations:

- Device sends `WAKE` as broadcast to `FF:FF:FF:FF:FF:FF` with jittered repeats (e.g., 3 frames).
- Neighbors refresh tables; device selects a new parent from BEACON/HELLO updates.

---

## 4) Node Failures, Churn, and Partitions

### 4.1 Parent dies (subtree isolation)

Failure:

- Single-parent tree makes the subtree temporarily unreachable for DOWN.

Mitigations:

- Fast parent expiry + re-parenting driven by BEACON.
- Gateway retries + end-to-end ACK for critical commands.

### 4.2 Partition and heal

Failure:

- Metrics go stale; heal causes congestion.

Mitigations:

- Detect “no root signal”: if no BEACON for `T`, consider partitioned.
- After heal: gateway can send a stronger convergence wave (e.g., DECAY + BEACON burst), rate-limited.
- Drop stale payloads by age to avoid draining old queues.

---

## 5) Duplicates, Loops, and Storm Control

### 5.1 Dedup cache overflow

Failure:

- A small cache forgets quickly; duplicates re-circulate.

Mitigations:

- Increase dedup capacity (e.g., 256–512 entries is often feasible).
- Store compact keys (64-bit hash of `(originMac,msgId)`), age out after 5–10s.

### 5.2 TTL bugs

Failure:

- Underflow can create “immortal” packets.

Mitigations:

- Always `if ttl == 0 drop` before decrement.
- Use saturating arithmetic.

### 5.3 `msgId/seq` wraparound

Failure:

- Legit traffic is dropped as duplicate/replay.

Mitigations:

- Dedup key must include `originMac`.
- Make `seq` session-aware (32-bit, or 16-bit + rebootCounter).

---

## 6) Congestion, Queues, and Backpressure

### 6.1 Forward queue growth

Failure:

- Burst traffic triggers RAM spikes and watchdog resets.

Mitigations:

- Hard queue limits (5–20).
- Priority drop (control > command > telemetry, or explicit `msgType` priorities).
- Drop by age (stale packets are not forwarded).

### 6.2 Gateway ↔ HUB UART bottleneck

Failure:

- Mesh delivers, UART becomes the choke point.

Mitigations:

- UART framing + checksum.
- Backpressure signals (queue occupancy) and token-bucket rate limiting on HUB.

### 6.3 Retry storms

Failure:

- Missing ACK causes repeated DOWN retries that saturate the mesh.

Mitigations:

- End-to-end ACK + exponential backoff.
- Stop rules: max attempts + max wall-clock window.
- For sleepy devices: only PULL.

---

## 7) MTU, Fragmentation, Ordering

### 7.1 Payload does not fit in one ESP‑NOW frame

Mitigations:

- Protocol fragmentation (`fragId`, `index`, `count`, `totalLen`) with bounded reassembly buffers and timeouts.
- Identity broadcasts that include `DEV_CERT` + `Ed25519` signature can exceed a single ESP-NOW frame in practice (e.g., `DEV_CERT` often ~110–130 bytes + 64-byte signature, plus encoding). Prefer `certId` caching and avoid repeating full certs on broadcast.

### 7.2 Out-of-order delivery

Mitigations:

- Commands must be idempotent and strictly ordered via `seq`.
- Telemetry can tolerate out-of-order but should carry a local sensor sequence.

---

## 8) Counter and Metric Overflow

Failures:

- `q_up/q_total` overflow makes a good node look bad.
- `decayEpoch` wrap breaks convergence.

Mitigations:

- Saturating arithmetic for charges + periodic decay.
- Use wide epoch (e.g., 32-bit) or treat reboot/upgrade as a new epoch domain.

---

## 9) Security (Control-Plane)

### 9.1 Control spoofing without offline identity

Failure:

- Forged `DECAY/BEACON/WAKE` destabilizes topology.

Mitigations:

- For `WAKE` (broadcast discovery): require `DEV_CERT` + `SIG_DEV` verification using `NET_CA_PUB`.
- For topology-critical gateway control (`DECAY/BEACON`): require gateway identity (gateway `DEV_CERT` + signature) and reject if `originMac` is not the Gateway.
- Optional: use `NET_GROUP_KEY` as a cheap filter for mesh-control traffic (DoS reduction only, not identity).

### 9.2 Key compromise

Mitigations:

- If `DEV_PRIV` is extracted from one device, attacker can impersonate only that device. Mitigate with short-lived `DEV_CERT` (expiry) and a signed revocation/deny-list distributed by the Gateway.
- If `NET_GROUP_KEY` is used and extracted, attacker can forge group-authenticated control. Treat it as disposable: rotate it and never rely on it for identity.
- Rate-limit control messages by type and by verified origin.

---

## 10) Test Matrix (Must-Pass Scenarios)

- Clique: 21+ nodes, ensure join + delivery still works.
- Sleepy: wake → WAKE broadcast → re-parent → command PULL.
- Parent death: subtree re-attaches within bounded time.
- Partition/heal: no flood storm; convergence completes.
- High load: dedup cache prevents loops; queues stay bounded.
- UART choke: backpressure prevents runaway buffering.
