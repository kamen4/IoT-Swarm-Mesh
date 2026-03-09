# Engine/Routers/Flooding

This folder contains the `FloodingPacketRouter`, the engine's default routing strategy.

---

## Files

| File              | Responsibility                                                                                                                                                                                                                                                                  |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `PacketRouter.cs` | `FloodingPacketRouter`  -  implements `IPacketRouter`. On each `Route` call it broadcasts a shallow clone of the packet to every device that is **visible** (not just connected) to the sender. Uses visibility to ensure delivery even before a connection graph is established. |

---

## Strategy characteristics

- **Reliability**  -  delivers to all reachable neighbours regardless of connection state.
- **Overhead**  -  produces O(visible-neighbours) clones per hop, growing exponentially in dense grids without TTL capping. High duplicate-delivery rate.
- **Use case**  -  engine default; useful as a baseline for benchmark comparisons.

---

## Parent

See `Engine/Routers/Documentation.md` for the full routing subsystem description.
