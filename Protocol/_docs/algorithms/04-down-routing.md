# DOWN Delivery — Tree-First Broadcast Concept

This document defines a **DOWN (gateway → device)** delivery strategy for the ESP‑NOW mesh.

Design goal: make DOWN reliable and simple by pushing the swarm to converge to a **tree-like structure**, so that forwarding is mostly **loop-free** and does not create duplicated storms.

It is intentionally compatible with the current design where **UP** works well with multi-path “swarm” routing.

---

## 1) Core Idea

- Keep **UP** as-is (resilient, multi-path) to preserve reliability in dynamic RF.
- Make **DOWN** run on a **tree/gradient** rooted at the Gateway.
- When a precise path is unknown or stale, DOWN becomes a **tree broadcast** (broadcast along tree edges only), not a mesh flood.

Tree broadcast properties:

- Each node forwards **only to its children**, never back to the parent.
- With a single parent per node, cycles are structurally avoided.
- Dedup `(originMac, msgId)` prevents re-forward on duplicates.

---

## 2) Tree Formation (Gateway Gradient)

### 2.1 BEACON (gateway root signal)

- Gateway periodically sends `BEACON` as a **link-layer broadcast** to `FF:FF:FF:FF:FF:FF`.
- `BEACON` SHOULD be authenticated so nodes can ignore spoofed roots.
  - Gateway signs BEACON using its per-device key and includes `DEV_CERT` (verified by `NET_CA_PUB`).
  - Optional (weaker): authenticate with `NET_GROUP_KEY` if you accept shared-key compromise risk.
- Payload carries a small scalar `g` (gateway distance/gradient).
  - Gateway sends `g = 0`.
  - A receiver sets `g_self = min(g_neighbor + 1)` and picks `parent = neighborWithMin(g_neighbor + 1 + rssiPenalty)`.

### 2.2 Parent selection and stability

- Parent switching must be conservative (hysteresis):
  - switch only if the new parent improves `g_self` by at least 2 hops (or improves by 1 hop with much better RSSI).
- Maintain `parentAge` and expire parent if not heard for `T_parent_dead`.

### 2.3 Children tracking

- A node considers another node its child if it observes that node selecting it as parent (via `HELLO/Wake` containing `parentMac`).
- Store children in a small `children[]` table with LRU eviction.

---

## 3) DOWN Forwarding Rules

### 3.1 Known destination

If a node receives DOWN with `dstMac = X`:

- If `X` is a direct neighbor: unicast to `X`.
- Else, forward **only to children**.

This is still broadcast-like (may traverse multiple branches), but it is constrained to the tree.

### 3.2 Unknown / recovery delivery

If gateway cannot map a path (or dst has not been seen recently):

- Send a DOWN as tree broadcast (same forwarding rule: children-only).
- Require **end-to-end ACK** from destination to stop retries.

### 3.3 ACK-based termination

- Destination sends `ACK(msgId)` as an end-to-end authenticated message back to gateway (UP direction).
- If a node hears a matching ACK soon after forwarding (short listen window), it may suppress further forwarding of the same `msgId`.
- Gateway stops retrying once ACK is received.

---

## 4) Sleepy Devices: Rejoin and Delivery

### 4.1 WAKE broadcast

On wake from deep sleep, a device sends a **link-layer broadcast** to `FF:FF:FF:FF:FF:FF`:

- `WAKE(originMac, parentMac?, lastKnownGradient?, capabilities)`

`WAKE` MUST be verifiable offline by neighbors (no server query):

- include `DEV_CERT` (or its id/hash) and `SIG_DEV` in payload
- neighbors validate `DEV_CERT` using `NET_CA_PUB` and verify `SIG_DEV`
- neighbors MAY require a quick unicast challenge-response to prevent replay

Neighbors use WAKE to:

- refresh neighbor tables,
- help the device quickly re-attach to the gradient/tree.

### 4.2 Command delivery model for sleepy nodes

- Sleepy nodes should not rely on push DOWN.
- Prefer a **PULL** model:
  - device wakes → sends WAKE/HELLO → gateway replies “pending commands available” → device pulls.

---

## 5) Interaction with Neighbor Limit

Because tables are small:

- Keep these entries **sticky**: `Gateway`, `parent`, top 2–3 stable children.
- Use LRU for the rest.
- Children table may be smaller than neighbors; prioritize children that recently forwarded ACK/UP traffic.

---

## 6) Trade-offs

- Tree-first DOWN minimizes duplicates and cycles, but reduces redundancy.
- To avoid isolating subtrees when a parent dies, rely on:
  - periodic BEACON,
  - fast parent expiry,
  - conservative but responsive re-parenting,
  - retries + ACK at gateway.

---

## 7) Minimal Protocol Additions (names only)

- `BEACON` (gateway broadcast, builds gradient)
- `WAKE` (device broadcast on wake)
- `ACK` (end-to-end acknowledgement for DOWN commands)
- Optional: `PULL` / `PULL_R` (sleepy device command retrieval)
