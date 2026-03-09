# Engine/Core

The `Core` folder contains the central runtime of the simulation: the singleton engine, its strongly-typed event definitions, and the exception thrown when the in-flight packet limit is exceeded.

---

## Files

| File                              | Responsibility                                                                                                                                                                                                                                        |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SimulationEngine.cs`             | Singleton tick-loop driver. Owns the device registry, packet priority queue, pluggable `Router` and `NetworkBuilder`, and `Topology`. Each `Tick()` call increments the tick counter, fires `TickEvent`, dispatches due packets, and raises `Ticked`. |
| `SimulationEvents.cs`             | All strongly-typed `EventArgs` used by `SimulationEngine` events: `PacketRegisteredEventArgs`, `PacketDeliveredEventArgs`, `PacketExpiredEventArgs`, `DeviceRegisteredEventArgs`, `DeviceRemovedEventArgs`, `TickedEventArgs`.                        |
| `PacketLimitExceededException.cs` | Thrown by `SimulationEngine.RegisterPacket` when the in-flight packet count reaches or exceeds `EffectivePacketLimit`. Carries `Limit`, `ActualCount`, and `AtTick`.                                                                                  |

---

## Key behaviors

- **Singleton pattern**  -  `SimulationEngine.Instance` and `SimulationStatistics.Instance` are process-level singletons; WASM runs a single instance shared by the whole browser session.
- **Packet limit guard**  -  prevents unbounded packet storms in flooding topologies. Auto-scales with device count unless `MaxActivePackets` is set manually.
- **Pluggable strategies**  -  `Router` (type `IPacketRouter`) and `NetworkBuilder` (type `INetworkBuilder`) can be replaced at runtime without restarting the simulation.

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
