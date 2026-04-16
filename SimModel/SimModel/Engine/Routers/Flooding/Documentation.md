# Engine/Routers/Flooding

This folder contains the `FloodingPacketRouter`, the baseline flooding strategy.

---

## Files

| File              | Responsibility                                                                                                                                                                                                                                                                  |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `PacketRouter.cs` | `FloodingPacketRouter`  -  implements `IPacketRouter`. On each `Route` call it broadcasts a shallow clone of the packet to the nearest visible neighbors (top-k, default k=10). Uses visibility to ensure delivery even before a connection graph is established, while keeping deterministic bounded fan-out. |

---

## Strategy characteristics

- **Reliability**  -  delivers to all reachable neighbours regardless of connection state.
- **Overhead**  -  produces O(k) clones per hop with k=10 by default. Duplicate-delivery rate is still high, but bounded compared to unrestricted flooding.
- **Use case**  -  useful as a baseline for benchmark comparisons.

---

## Parent

See `Engine/Routers/Documentation.md` for the full routing subsystem description.
