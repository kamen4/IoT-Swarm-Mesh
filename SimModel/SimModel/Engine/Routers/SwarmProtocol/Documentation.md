# Engine/Routers/SwarmProtocol

This folder contains the protocol-oriented router implementation that maps the
Protocol v1.0 swarm behavior into the Engine routing abstraction.

---

## Files

| File | Responsibility |
| ---- | -------------- |
| `SwarmProtocolPacketRouter.cs` | `SwarmProtocolPacketRouter` implementation of `IPacketRouter`. Applies direction-aware routing on a nearest-visible top-k window (default k=10): UP bootstrap fan-out until charge signal appears, then best-neighbor by observed q_up; DOWN unicast/tree-first and broadcast children-first with deterministic fallback by q_total. Stamps previous-hop MAC and charge metadata on every clone. |

---

## Routing rules implemented

- UP path:
  - Use nearest-visible top-k neighbors as a deterministic candidate set.
  - Bootstrap stage: if all observed `q_up` values are zero, fan-out over the same top-k window as flooding.
  - Prefer direct delivery when destination is currently visible.
  - After charge signal appears, select one best visible neighbor by sender-observed `q_up`.
  - Tie-break deterministically by MAC order.
- DOWN path:
  - Unicast: direct if destination visible, otherwise children-first (neighbors whose selected parent is the sender).
  - Broadcast: children-first fan-out with single-neighbor fallback by `q_total`.
- Envelope stamping:
  - Each clone receives `PreviousHop`, `PreviousHopMac`, `AdvertisedCharge`, and `DecayEpochHint` from the sender.

---

## Parent

See `Engine/Routers/Documentation.md` for the full routing subsystem overview.
