# Engine/Routers/SmartFlooding

This folder contains the `SmartFloodingPacketRouter`, the WebApp's default routing strategy and the improved alternative to `FloodingPacketRouter`.

---

## Files

| File                           | Responsibility                                                                                                                                                                                                                                                                                            |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SmartFloodingPacketRouter.cs` | `SmartFloodingPacketRouter`  -  implements `IPacketRouter`. Applies three optimisations over naive flooding: (1) direct delivery if the destination is a visible neighbour, skipping the full broadcast; (2) no reverse-path forward back to `PreviousHop`; (3) no echo back to the original `From` device. |

---

## Strategy characteristics

- **Reliability**  -  guaranteed delivery in any connected topology with sufficient TTL.
- **Overhead**  -  substantially fewer clones than naive flooding; in sparse topologies may reach near-unicast efficiency.
- **Use case**  -  WebApp default (`SimulationConfig.SelectedRouter`); set by `SimulationService` on construction.

---

## Parent

See `Engine/Routers/Documentation.md` for the full routing subsystem description.
