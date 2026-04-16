# Engine/Routers/SmartFlooding

This folder contains the `SmartFloodingPacketRouter`, an improved alternative to `FloodingPacketRouter`.

---

## Files

| File                           | Responsibility                                                                                                                                                                                                                                                                                            |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SmartFloodingPacketRouter.cs` | `SmartFloodingPacketRouter`  -  implements `IPacketRouter`. Applies bounded candidate selection and three optimisations over naive flooding: nearest-visible top-k window (default k=10), direct delivery if destination is visible, no reverse-path back to `PreviousHop`, and no echo back to the original `From` device. |

---

## Strategy characteristics

- **Reliability**  -  guaranteed delivery in any connected topology with sufficient TTL.
- **Overhead**  -  substantially fewer clones than naive flooding due to direct delivery, loop suppression, and top-k visible-neighbor cap.
- **Use case**  -  selectable runtime strategy for lower fan-out than naive flooding.

---

## Parent

See `Engine/Routers/Documentation.md` for the full routing subsystem description.
