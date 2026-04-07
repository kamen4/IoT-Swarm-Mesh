# Swarm routing (charge-based)

The mesh is self-organizing. Devices do not store global routes. Each device stores only neighbor MACs and two charge values.

## Definitions

- **Gateway** is the ESP device physically connected to HUB over UART. Its MAC address is fixed and considered the root of the mesh for `UP` direction.
- `q_up` - charge used to route messages **towards gateway** (`dir=UP`). Intuition: higher means "more traffic successfully flows to gateway through this node".
- `q_total` - charge used to route messages **from gateway** (`dir=DOWN`). Intuition: higher means "more central / more traffic goes through this node overall".

Each device maintains:

- Neighbor table: `neighbors[mac] = { q_up, q_total, lastSeen }`
- Local charges: `q_up_self`, `q_total_self`
- `lastDecayEpoch` - last applied decay epoch number
- `seenCache` - fixed-size cache of last `N` `(originMac, msgId)`

## Charge advertisement

Each hop advertises its local charge to neighbors:

- For a forwarded `dir=UP` packet, sender sets `ROUTING_HEADER.charge = q_up_self`.
- For a forwarded `dir=DOWN` packet, sender sets `ROUTING_HEADER.charge = q_total_self`.

When a device receives a packet from neighbor `A`, it updates neighbor charges from the routing header:

- If `dir=UP` then `neighbors[A].q_up = max(neighbors[A].q_up, charge)`
- If `dir=DOWN` then `neighbors[A].q_total = max(neighbors[A].q_total, charge)`

Implementations MAY use smoothing instead of `max`, but the protocol expects monotonic convergence.

## Charge accumulation

Charges grow based on how much traffic passes through a node:

- When a device forwards a `dir=UP` message: increment `q_up_self` and `q_total_self`.
- When a device forwards a `dir=DOWN` message: increment `q_total_self`.

Exact increment function is implementation-defined.

## Forwarding rule (top 50%)

When a device receives a packet that is not for itself:

1. Drop if `ttl == 0`.
2. Drop if `(originMac, msgId)` is already in `seenCache`.
3. Otherwise, decrement `ttl` and forward.

Forwarding target selection:

- If `dstMac` is a direct neighbor: unicast to `dstMac`.
- Otherwise, select neighbors excluding `prevHopMac`.
  - For `dir=UP`: sort by `neighbors[mac].q_up` descending.
  - For `dir=DOWN`: sort by `neighbors[mac].q_total` descending.
  - Forward to the top `ceil(0.5 * neighborCount)` neighbors (minimum 1).

This is not a broadcast; it is a swarm-propagation step.

## Charge decay (network-wide)

To prevent unbounded charge growth and to provide a global dedup primitive, the network supports a decay epoch.

Mesh-control message:

- `msgType = DECAY`
- Payload: `{ decayEpoch, percent }`

Rules:

- Every device stores `lastDecayEpoch`.
- Upon receiving `DECAY` with epoch `E`:
  - If `E <= lastDecayEpoch`: ignore.
  - If `E > lastDecayEpoch`: apply decay `(E - lastDecayEpoch)` times:
    - `q_up_self *= (1 - percent)`
    - `q_total_self *= (1 - percent)`
    - also decay stored neighbor charges
  - Set `lastDecayEpoch = E`.
  - Forward this `DECAY` message further using the standard forwarding rules (dedup by `(originMac,msgId)` + `ttl`).

Devices MAY also apply decay locally on startup by requesting/learning the latest epoch (using `decayEpochHint` observed in traffic).

## Minimal message types (network level)

- `HELLO` - optional presence announcement (helps `neighbors.lastSeen`).
- `BEACON` - gateway-originated link-layer broadcast used to help the mesh converge after join/reboot/partition.
- `WAKE` - device-originated link-layer broadcast on wake from deep sleep (helps re-attach after sleep/path loss).
- `DATA_UP` - device -> gateway (telemetry/events/responses).
- `DATA_DOWN` - gateway -> device (commands/queries).
- `DECAY` - network-wide charge decay.
