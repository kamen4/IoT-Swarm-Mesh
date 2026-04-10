# DOWN Delivery — Tree-First Broadcast Concept

This document defines a **DOWN (gateway → device)** delivery strategy for the ESP‑NOW mesh.

Design goal: make DOWN reliable and simple by pushing the swarm to converge to a **tree-like structure**, so that forwarding is mostly **loop-free** and does not create duplicated storms.

It is intentionally compatible with the current design where **UP** works well with multi-path “swarm” routing.

---

## 1) Core Idea

- Keep **UP** as-is (resilient, multi-path) to preserve reliability in dynamic RF.
- Make **DOWN** run on a **single-parent tree** rooted at the Gateway.
- The tree is induced by **accumulated charges** (`q_total`) rather than a hop-gradient.
- When a precise path is unknown or stale, DOWN becomes a **tree broadcast** (broadcast along tree edges only), not a mesh flood.

Tree broadcast properties:

- Each node forwards **only to its children**, never back to the parent.
- A child edge is used only if the child is “charged enough” (forward-eligible): `q_total(child) >= q_forward`.
- With a single parent per node, cycles are structurally avoided.
- Dedup `(originMac, msgId)` prevents re-forward on duplicates.

---

## 2) Tree Formation (Charge-Induced Parent Pointers)

### 2.1 BEACON (gateway root signal)

- Gateway periodically sends `BEACON` as a **link-layer broadcast** to `FF:FF:FF:FF:FF:FF`.
- `BEACON` is mesh-control and is not end-to-end authenticated (`TAG = 0`); receivers MUST treat it as an untrusted convergence hint (see [Identity & Security](05-identity-security.md)).
- Payload carries an implementation-defined convergence hint for charge-based tree formation (e.g., gateway’s current `q_total` and/or a recommended `q_forward`).

Charge learning used by the charge-induced tree:

- For forwarded `dir=DOWN` frames, sender advertises `ROUTING_HEADER.charge = q_total_self` (see [UP Routing](03-up-routing.md)).
- Neighbors maintain `neighbors[mac].q_total = max(neighbors[mac].q_total, charge)`.
- Define `q_forward` as a protocol/implementation parameter: the minimum neighbor charge required to forward during DOWN tree-broadcast.

### 2.2 Parent selection and stability

- Parent switching must be conservative (hysteresis):
  - switch only if the new parent is significantly better (e.g., charge delta ≥ `Δq`, or smaller delta with much better RSSI).
- Parent selection should prefer the eligible neighbor with the highest observed `q_total` (RSSI only as a tie-break), and require strict improvement (`q_total(parent) > q_total(self)`) to avoid cycles in the ideal model.
- Maintain `parentAge` and expire parent if not heard for `T_parent_dead`.

### 2.3 Children tracking

- A node considers another node its child if it observes that node selecting it as parent (via `HELLO/Wake` containing `parentMac`).
- Store children in a small `children[]` table with LRU eviction.

---

## 3) DOWN Forwarding Rules

### 3.1 Known destination

If a node receives DOWN with `dstMac = X`:

- If `X` is a direct neighbor: unicast to `X`.
- Else, forward **only to children** that are forward-eligible (`neighbors[child].q_total >= q_forward`).

This is still broadcast-like (may traverse multiple branches), but it is constrained to the tree.

### 3.2 Unknown / recovery delivery

If gateway cannot map a path (or dst has not been seen recently):

- Send a DOWN as tree broadcast (same forwarding rule: eligible-children-only).
- Require **end-to-end ACK** from destination to stop retries.

### 3.3 ACK-based termination

- Destination sends `ACK(msgId)` as an end-to-end authenticated message back to gateway (UP direction).
- If a node hears a matching ACK soon after forwarding (short listen window), it may suppress further forwarding of the same `msgId`.
- Gateway stops retrying once ACK is received.

---

## 4) Sleepy Devices: Rejoin and Delivery

### 4.1 WAKE broadcast

On wake from deep sleep, a device sends a **link-layer broadcast** to `FF:FF:FF:FF:FF:FF`:

- `WAKE(originMac, parentMac?, lastKnownChargeTotal?, capabilities)`

`WAKE` is mesh-control and is not end-to-end authenticated (`TAG = 0`). Receivers MUST treat it as an untrusted hint:

- apply rate limiting and deduplication;
- do not treat WAKE as proof of network membership;
- if a membership decision is required, perform a server-assisted peer check (see [Identity & Security](05-identity-security.md)).

Neighbors use WAKE to:

- refresh neighbor tables,
- help the device quickly re-attach to the charge-induced tree.

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
- `q_forward` may temporarily exclude cold / low-activity nodes from DOWN tree-broadcast; those nodes should rely on WAKE + PULL.
- To avoid isolating subtrees when a parent dies, rely on:
  - periodic BEACON,
  - fast parent expiry,
  - conservative but responsive re-parenting driven by charge updates,
  - retries + ACK at gateway.

---

## 7) Minimal Protocol Additions (names only)

- `BEACON` (gateway broadcast, helps charge-tree convergence)
- `WAKE` (device broadcast on wake)
- `ACK` (end-to-end acknowledgement for DOWN commands)
- Optional: `PULL` / `PULL_R` (sleepy device command retrieval)
