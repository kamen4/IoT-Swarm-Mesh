# Mathematical Model of the IoT Swarm Mesh Protocol

This document formalizes the routing-related parts of the protocol as specified in this documentation set.

Scope:

- UP routing (swarm, charge-based): [UP Routing — Swarm (charge-based)](../algorithms/03-up-routing.md)
- DOWN delivery (tree-first, charge-induced tree): [DOWN Delivery — Tree-First Broadcast Concept](../algorithms/04-down-routing.md)
- Envelope + dedup primitives: [Message Envelope](../algorithms/02-message-envelope.md)
- Terms: [Glossary](../00-glossary.md)

Non-goals:

- This is not a radio/PHY model.
- Cryptography is modeled only as an authenticity predicate (who can forge what).

---

## 1) Network and Time Model

Let $V$ be the finite set of devices (nodes). One distinguished node $r \in V$ is the **Gateway** (root).

At any time, the physical neighbor relation is modeled by an undirected graph

$$
G = (V, E), \quad E \subseteq \{\{u,v\} : u,v \in V, u \neq v\}.
$$

If $\{u,v\} \in E$, then $u$ and $v$ can exchange ESP-NOW frames directly (single hop).

### 1.1 Rounds / iterations (for analysis)

To reason about convergence, we use a round-based abstraction:

- In each round, every node may receive some set of frames from neighbors and may transmit frames to neighbors.
- For the convergence theorem in [Charge-Induced Tree Theorem (DOWN Routing)](theorem.md), we will assume a stable interval where $G$ is fixed and enough traffic/announcements propagate so that neighbor-charge views and parent choices can stabilize.

The protocol itself is asynchronous; the round model is only a proof tool.

---

## 2) Message/Frame Abstraction

Each forwarded frame contains:

- **Immutable** fields (do not change hop-to-hop):
  - direction $\textsf{dir} \in \{\textsf{UP},\textsf{DOWN}\}$
  - message type $\textsf{msgType}$
  - $\textsf{origin} \in V$ (origin MAC)
  - $\textsf{dst} \in V \cup \{\textsf{BCAST}\}$ (destination MAC or broadcast)
  - message identifier $\textsf{msgId} \in \{0,1,\dots,2^{16}-1\}$
- **Mutable** per-hop fields:
  - $\textsf{ttl} \in \{0,1,\dots,T\}$
  - $\textsf{prevHop} \in V$
  - $\textsf{charge} \in \{0,1,\dots,2^{16}-1\}$
  - $\textsf{decayEpochHint}$

Deduplication key:

$$
\textsf{key}(m) = (\textsf{origin},\textsf{msgId}).
$$

**Dedup rule (normative):** each node maintains a fixed-size cache $\mathcal{D}_u$ of the last $N$ seen keys; if an incoming frame has a key already in $\mathcal{D}_u$, it is dropped.

**TTL rule (normative):** a node drops a frame if $\textsf{ttl}=0$; otherwise it decrements $\textsf{ttl}\leftarrow \textsf{ttl}-1$ before forwarding.

---

## 3) Per-Node State

Each node $u \in V$ maintains local state variables.

### 3.1 Neighbor table (local view)

For each neighbor $v$ that $u$ currently tracks, $u$ stores:

- $Q^{\uparrow}_u(v)$: estimated UP charge of neighbor $v$
- $Q^{\mathrm{tot}}_u(v)$: estimated total charge of neighbor $v$
- $\mathrm{lastSeen}_u(v)$: last observation time (not used in proofs below)

The set of tracked neighbors is a bounded set $\mathcal{N}_u \subseteq \{v : \{u,v\}\in E\}$.

### 3.2 Charges

Node-local charges (as in [UP Routing — Swarm (charge-based)](../algorithms/03-up-routing.md)):

- $q^{\uparrow}_u \in \mathbb{R}_{\ge 0}$ (or integer, implementation-defined scaling)
- $q^{\mathrm{tot}}_u \in \mathbb{R}_{\ge 0}$

These are **advertised** hop-by-hop via the mutable field `ROUTING_HEADER.charge`.

### 3.3 Decay epoch

- $e_u \in \mathbb{N}$: `lastDecayEpoch` applied at node $u$.

### 3.4 DOWN-tree state (charge-induced parent/children)

As in [DOWN Delivery — Tree-First Broadcast Concept](../algorithms/04-down-routing.md):

- parent pointer $p_u \in V \cup \{\bot\}$
- children set $C_u \subseteq V$ (bounded table; in the formal theorem we treat it as “best effort cache” unless stated otherwise)

We also fix a forwarding eligibility threshold $q_{\mathrm{forward}} \ge 0$ (an implementation parameter).

Gateway invariant: $p_r = \bot$.

---

## 4) UP Routing Dynamics (Charge-Based Swarm)

UP routing is hop-by-hop greedy best-neighbor forwarding (top-1). It does not require global routes and is not claimed to converge to a tree.

### 4.1 Charge advertisement and neighbor-charge update

When $u$ receives a frame from neighbor $v$ with per-hop advertised `charge = c`:

- If $\textsf{dir}=\textsf{UP}$, then

$$
Q^{\uparrow}_u(v) \leftarrow \max\{Q^{\uparrow}_u(v),\; c\}
$$

- If $\textsf{dir}=\textsf{DOWN}$, then

$$
Q^{\mathrm{tot}}_u(v) \leftarrow \max\{Q^{\mathrm{tot}}_u(v),\; c\}
$$

The spec allows smoothing instead of max, but expects monotonic convergence.

### 4.2 Charge accumulation on forwarding

When node $u$ forwards a frame:

- If forwarding a `dir=UP` frame:
  - increment $q^{\uparrow}_u \leftarrow q^{\uparrow}_u + \Delta^{\uparrow}_u$
  - increment $q^{\mathrm{tot}}_u \leftarrow q^{\mathrm{tot}}_u + \Delta^{\mathrm{tot}}_u$
  - set outgoing advertised charge $\textsf{charge} \leftarrow q^{\uparrow}_u$

- If forwarding a `dir=DOWN` frame:
  - increment $q^{\mathrm{tot}}_u \leftarrow q^{\mathrm{tot}}_u + \Delta^{\downarrow}_u$
  - set outgoing advertised charge $\textsf{charge} \leftarrow q^{\mathrm{tot}}_u$

The increments $\Delta$ are implementation-defined (e.g., constants 1, or weighted by message type, etc.).

### 4.3 Forwarding (top-1 rule)

For frames that are not for $u$ itself:

1. Drop if `ttl=0`.
2. Drop if the key $(\textsf{origin},\textsf{msgId}) \in \mathcal{D}_u$.
3. Otherwise, add key to $\mathcal{D}_u$, decrement TTL, and forward.

Neighbor selection for swarm-forwarding:

- If `dst` is a direct neighbor (i.e., in $\mathcal{N}_u$), unicast directly.
- Else if `dir=DOWN` and `dst \ne BCAST`, then **do not swarm-forward**: use DOWN tree rules (Section 5).
- Else swarm-forward to exactly one neighbor excluding $\textsf{prevHop}$.
  - Let $\mathcal{S}_u$ be the candidate set (neighbors excluding $\textsf{prevHop}$). If $\mathcal{S}_u = \varnothing$, do not forward.
  - For `dir=UP`, choose $v^* \in \arg\max_{v\in\mathcal{S}_u} Q^{\uparrow}_u(v)$.
  - For `dir=DOWN` broadcast/control (`dst=BCAST`), choose $v^* \in \arg\max_{v\in\mathcal{S}_u} Q^{\mathrm{tot}}_u(v)$.
  - Forward to $v^*$.

Tie-breaking among equal charges is not specified by the protocol; for deterministic analysis, a fixed tie-break (e.g. MAC order) can be assumed.

### 4.4 Interpretation of charges (informal)

- Higher $q^{\uparrow}$ tends to mean “this node has recently forwarded more UP traffic”, hence is likely on or near successful paths to the gateway.
- The `max` update makes neighbor estimates monotone non-decreasing between decay epochs.
- Without decay, the system can accumulate unbounded values; DECAY resets the scale.

---

## 5) DOWN Routing Dynamics (Charge-Induced Tree)

DOWN delivery is built on a single-parent structure induced by a scalar **tree charge**.

In this model, we use the already-defined total charge $q^{\mathrm{tot}}$ as the tree charge (it is advertised in DOWN frames and tends to correlate with being on active paths).

### 5.1 Forward-eligibility threshold

Fix $q_{\mathrm{forward}} \ge 0$.

A node is **forward-eligible** if:

$$
q^{\mathrm{tot}}_u \ge q_{\mathrm{forward}}.
$$

The theorem in `theorem.md` is stated for the forward-eligible subset (and becomes a full spanning-tree claim when all nodes are forward-eligible).

### 5.2 Parent selection from neighbor charges

Each node $u$ maintains neighbor charge estimates $Q^{\mathrm{tot}}_u(v)$ updated by Section 4.1.

Define the set of eligible parent candidates:

$$
\mathcal{P}_u = \{\, v \in \mathcal{N}_u : Q^{\mathrm{tot}}_u(v) > q^{\mathrm{tot}}_u \ \wedge\ Q^{\mathrm{tot}}_u(v) \ge q_{\mathrm{forward}} \,\}.
$$

When $\mathcal{P}_u \neq \emptyset$, node $u$ chooses

$$
p_u \in \arg\max_{v\in \mathcal{P}_u} \bigl(Q^{\mathrm{tot}}_u(v) - \pi_u(v)\bigr)
$$

where $\pi_u(v) \ge 0$ is an RSSI-derived penalty (used only for tie-breaking among near-equal candidates).

If $\mathcal{P}_u=\emptyset$, a node may keep $p_u=\bot$ (or keep its previous parent in an implementation).

The key invariant used in the theorem is that whenever $p_u$ is set, the chosen parent has strictly higher tree charge:

$$
q^{\mathrm{tot}}_{p_u} > q^{\mathrm{tot}}_u.
$$

### 5.3 Stability / hysteresis

Parent switching may be conservative (hysteresis), delaying changes unless the improvement is significant.

In proofs we model this as: if a strictly better parent is persistently available, the node eventually switches.

### 5.4 Tree-broadcast forwarding rule (children-only + threshold)

For DOWN unicast traffic with destination $d \ne \textsf{BCAST}$:

- If $d$ is a direct neighbor: unicast to $d$.
- Otherwise forward only to children $v \in C_u$ that are forward-eligible (based on stored estimates):

$$
Q^{\mathrm{tot}}_u(v) \ge q_{\mathrm{forward}}, \quad v \in C_u.
$$

As in UP routing, this is combined with TTL + hop-by-hop deduplication; a node forwards a given key at most once.

---

## 6) DECAY (Network-Wide Charge Decay)

DECAY is a mesh-control message disseminated using swarm forwarding.

A DECAY message carries an epoch number $E$ and a decay factor $p \in (0,1)$ (called `percent` in docs).

When node $u$ receives DECAY with epoch $E$:

- If $E \le e_u$: ignore.
- If $E > e_u$: apply the decay $E-e_u$ times:

$$
q^{\uparrow}_u \leftarrow (1-p)^{E-e_u} q^{\uparrow}_u,
\quad
q^{\mathrm{tot}}_u \leftarrow (1-p)^{E-e_u} q^{\mathrm{tot}}_u,
$$

and similarly decay all stored neighbor charges $Q^{\uparrow}_u(v)$ and $Q^{\mathrm{tot}}_u(v)$.

Then set $e_u \leftarrow E$ and forward the DECAY further (subject to TTL + dedup).

### 6.1 DECAY as a global “reset” primitive

- Between DECAY epochs, charges monotonically increase (locally) and neighbor estimates monotonically increase (via `max`).
- DECAY periodically shrinks all stored charges to prevent overflow and reduce the effect of stale history.

---

## 7) Authenticity Model (for routing-critical control)

For the purpose of routing proofs, we use a predicate $\textsf{Auth}(m)$ meaning “the message is accepted as authentic by receivers”.

Routing-critical assumptions mentioned in docs:

- BEACON/DECAY/WAKE are mesh-control broadcasts and are not end-to-end authenticated by `TAG`; in this design there are no offline certificates/CA keys, so receivers must treat them as untrusted hints (membership checks require the server).
- If a proof requires an authenticity assumption (e.g., “only the real gateway emits BEACON”), we will state it explicitly as an external assumption, not as a property of on-mesh messages.

In proofs we will explicitly state whether authenticity is assumed.

---

## 8) What “Convergence to a Tree” Means Here

The protocol has two routing sub-systems:

1. **UP** is hop-by-hop best-neighbor forwarding (top-1) and is not intended/claimed to converge to a tree.
2. **DOWN** uses a single parent per node induced by accumulated charges (subject to a forwarding eligibility threshold $q_{\mathrm{forward}}$), producing a directed structure pointing “toward the root”.

Accordingly, the theorem in `theorem.md` is about the charge-induced parent pointers $(p_u)$ defining a gateway-rooted spanning tree on the forward-eligible nodes, and about DOWN tree-broadcast being loop-free / duplicate-free under suitable assumptions.

---

## 9) Implementation Parity Notes (Python)

For operational parity with the current Python simulator:

- canonical phase order is documented in [Simulation Pipeline](../mitigations/simulation-pipeline.md),
- practical parameter ranges are documented in [Convergence Tuning](../mitigations/convergence-tuning.md),
- theorem check semantics (`pending/pass/fail`, first-pass vs sustained-pass) are documented in [Charge-Induced Tree Theorem](theorem.md).

These references are normative for batch-study reproducibility.
