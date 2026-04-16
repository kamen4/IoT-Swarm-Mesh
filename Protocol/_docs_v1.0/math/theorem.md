# Charge-Induced Tree Theorem (DOWN Routing)

This file states and proves a structural theorem for the **DOWN** delivery subsystem when it uses a **single-parent tree induced by accumulated charges**, as described in:

- [DOWN Delivery — Tree-First Broadcast Concept](../algorithms/04-down-routing.md) (charge-tree concept, `q_forward`, parent selection, children-only forwarding)
- [UP Routing — Swarm (charge-based)](../algorithms/03-up-routing.md) (how `q_total` is accumulated and advertised)
- [Glossary](../00-glossary.md) (charge terminology)
- [Message Envelope](../algorithms/02-message-envelope.md) (dedup/ttl primitives)

Important scope note:

- UP routing (`q_up/q_total` swarm) uses hop-by-hop best-neighbor (top-1) forwarding and is **not** claimed to converge to a tree.
- This theorem concerns the **charge-induced parent pointers** used to make DOWN tree-broadcast loop-free and duplicate-free.

---

## 1) Definitions

Let $G=(V,E)$ be the (undirected) neighbor graph. Let $r\in V$ be the Gateway.

Let $N(u)=\{v\in V : \{u,v\}\in E\}$ be the neighbor set.

Each node $u$ stores:

- $q_u \in \mathbb{R}_{\ge 0}$ (tree charge; in the protocol this is modeled by total charge $q^{\mathrm{tot}}_u$)
- $p_u \in V\cup\{\bot\}$ (parent)

Fix a forwarding eligibility threshold $q_{\mathrm{forward}}\ge 0$ and define the forward-eligible node set:

$$
V^+ = \{u\in V : q_u \ge q_{\mathrm{forward}}\}.
$$

Gateway invariant: $p_r=\bot$.

A directed parent graph induced by parent pointers (restricted to eligible nodes) is:

$$
T^+(p) = \{(u, p_u) : u\in V^+\setminus\{r\},\; p_u\in V^+\}.
$$

We say the eligible subsystem has converged to a **gateway-rooted spanning tree (on $V^+$)** if:

1. For all $u\in V^+\setminus\{r\}$, $p_u$ is defined (not $\bot$) and $(u,p_u)\in E$.
2. The directed graph with edges $(u\to p_u)$ is acyclic.
3. Following parent pointers from any $u\in V^+$ reaches $r$.

---

## 2) Assumptions (Explicit)

The protocol docs describe charge accumulation and neighbor learning but do not fully formalize timing/loss; to prove a clean tree property we assume a stable window.

**A1. Connectivity.** $G$ is connected.

**A2. Stable topology and charges.** During the analysis window, $G$ does not change and all $q_u$ are constant.

**A3. Enough charge propagation for neighbor knowledge.** There exists a notion of asynchronous rounds such that, in each round, every edge $\{u,v\}$ delivers at least one frame from $u$ to $v$ and from $v$ to $u$ that carries the sender’s current advertised tree charge (so neighbor tables can reflect the true $q$ values).

**A4. Eligible set is the target.** The DOWN tree theorem is claimed on $V^+$. If you want full-mesh coverage, additionally assume $V^+=V$ (i.e., every node has $q_u\ge q_{\mathrm{forward}}$).

**A5. Unique root maximum.** The gateway is the unique strict maximum of charge among eligible nodes:

$$
q_r > q_u \quad \forall u\in V^+\setminus\{r\}.
$$

**A6. Local progress (no other local maxima).** Every eligible non-root node has at least one eligible neighbor with strictly higher charge:

$$
\forall u\in V^+\setminus\{r\}\;\exists v\in N(u)\cap V^+ : q_v > q_u.
$$

**A7. Parent choice consistent with charge.** Whenever $u\in V^+\setminus\{r\}$ selects a parent, it selects an eligible neighbor with strictly higher charge (ties broken deterministically, e.g., by MAC order):

$$
p_u \in \arg\max_{v\in N(u)\cap V^+\, :\, q_v > q_u} \bigl(q_v\bigr).
$$

In particular, this implies $q_{p_u} > q_u$.

(If an implementation uses RSSI penalties, they must only break ties among candidates that still satisfy $q_v>q_u$.)

**A8. Hysteresis does not block improvements forever.** Conservative switching may delay parent adoption, but if a strictly better eligible parent is persistently available, the node eventually switches.

---

## 3) Theorem

**Theorem (Eligible charges induce a spanning tree; DOWN tree-broadcast has no loops/duplicates).**

Under assumptions A1–A8, there exists a time after which:

1. For every eligible node $u\in V^+\setminus\{r\}$, the parent pointer $p_u$ is defined and satisfies $p_u\in N(u)\cap V^+$ and $q_{p_u} > q_u$.
2. The directed graph $T^+(p)$ is a spanning tree rooted at $r$ over the vertex set $V^+$.

Moreover, consider a DOWN tree-broadcast of a message $m$ restricted to $V^+$:

- The gateway sends $m$ to all children (neighbors $c$ with $p_c=r$).
- Any eligible node $u\ne r$ forwards $m$ at most once, and only to its eligible children $c$ with $p_c=u$ (never to $p_u$).

Then:

- The forwarding process cannot contain a loop.
- Each eligible node receives $m$ at most once; if every tree edge delivers at least one copy of $m$, then each eligible node receives $m$ exactly once.

---

## 4) Proof

The proof has two parts:

- (i) parent pointers induced by a strict charge potential form a rooted tree on $V^+$
- (ii) tree-broadcast over that tree is loop-free and duplicate-free

### 4.1 Lemma: parent edges strictly increase charge

For any eligible non-root node $u\in V^+\setminus\{r\}$, Assumption A7 yields:

$$
q_{p_u} > q_u.
$$

Therefore, along any directed path $u \to p_u \to p_{p_u} \to \cdots$, the charge strictly increases at every step.

### 4.2 Lemma: the parent graph is acyclic

Assume for contradiction there is a directed cycle

$$
u_0 \to u_1 \to \cdots \to u_{k-1} \to u_0.
$$

By Lemma 4.1 we would have

$$
q_{u_1} > q_{u_0},\; q_{u_2} > q_{u_1},\; \dots,\; q_{u_0} > q_{u_{k-1}},
$$

which implies $q_{u_0} > q_{u_0}$, a contradiction. Hence $T^+(p)$ is acyclic.

### 4.3 Lemma: every eligible node reaches the gateway

Start from any $u\in V^+$. If $u=r$, we are done.

Otherwise, repeatedly follow parent pointers. By Lemma 4.1 the sequence of charges strictly increases, so it cannot visit any node twice (in particular, it cannot loop).

Because $V^+$ is finite, this sequence must terminate at some node $w\in V^+$ with no eligible neighbor of strictly higher charge.

By Assumption A6, the only such node is the gateway $r$. Therefore every eligible node reaches $r$ by following parent pointers.

### 4.4 Spanning tree conclusion

Each eligible non-root node has exactly one outgoing parent edge, and by Lemma 4.2 the directed parent graph is acyclic, so it is a directed forest.

By Lemma 4.3, all components have the same root $r$, hence the forest is a single spanning tree rooted at $r$ over $V^+$.

This proves items (1)–(2) of the theorem.

### 4.5 Lemma: DOWN tree-broadcast is loop-free

A DOWN tree-broadcast forwards only from a node to its children in the rooted tree and never to its parent.

Because the underlying structure is a tree (acyclic), any sequence of forwardings that follows tree edges cannot form a cycle. This proves item (3).

### 4.6 Lemma: DOWN tree-broadcast is duplicate-free (on $V^+$)

In a rooted tree, every node $u\in V^+\setminus\{r\}$ has exactly one parent.

Therefore there is exactly one simple path from $r$ to $u$. Under the broadcast rule, the only node that can send $m$ to $u$ is its parent $p_u$.

Hence $u$ can receive $m$ at most once. If every tree edge delivers at least one copy of $m$, then each node receives exactly once. This proves item (4).

---

## 5) Notes and Practical Caveats

1. This theorem is **structural**: it shows that if the charge field has a unique maximum at the gateway and strictly increases toward it (A5–A7), then the induced parent pointers form a tree and DOWN broadcast is loop-free / duplicate-free.

2. The theorem intentionally separates “tree correctness given a charge field” from “does the runtime charge accumulation procedure always produce such a field”. The latter depends on traffic patterns, decay, and implementation details.

3. In simulation, early rounds can have only the gateway in the eligible set. In that warm-up phase, theorem checks are best interpreted as **pending** rather than proof of correctness, because assumptions A6/A7 are not yet exercised on non-root eligible nodes.

4. In the real protocol, packets can be duplicated by retransmissions and concurrent deliveries; hop-by-hop deduplication by `(originMac, msgId)` ensures a node forwards at most once even if it receives multiple copies.

5. The threshold $q_{\mathrm{forward}}$ makes the theorem naturally apply to an “active / charged” subset of the mesh. Sleepy or newly joined nodes that do not yet satisfy $q_u\ge q_{\mathrm{forward}}$ should rely on WAKE + PULL delivery rather than being targeted by a tree-broadcast.

6. Python simulation verification semantics (in `_theoreme_sim_py`) are:
	- `verificationState = pending` when only gateway (or fewer) is eligible; this is not a theorem failure.
	- `verificationState = pass` only when A5/A6/A7, Lemma 4.1/4.2/4.3, and spanning assignment all pass.
	- `verificationState = fail` in all other non-pending cases.

7. For diagnostics, the simulation reports both first-pass and sustained-pass rounds:
	- first-pass round: first round where a check became true,
	- sustained-pass round: earliest round from which that check stayed true until the end.

8. `cycleWitness` in Lemma 4.2 is an explicit node sequence extracted from parent-pointer traversal when a cycle is found. `unreachable` in Lemma 4.3 lists eligible nodes that cannot reach gateway by parent chaining.
