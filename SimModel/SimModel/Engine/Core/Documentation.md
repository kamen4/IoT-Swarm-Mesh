# Engine/Core

The `Core` folder contains the central runtime of the simulation: the singleton engine, its strongly-typed event definitions, and the exception thrown when the in-flight packet limit is exceeded.

---

## Files

| File                              | Responsibility                                                                                                                                                                                                                                        |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SimulationEngine.cs`             | Global-default tick-loop driver with optional scoped-instance override for isolated benchmark runs. Owns the device registry, packet priority queue, pluggable `Router` and `NetworkBuilder`, packet defaults (`DefaultPacketTtl`, `DefaultPacketTicksToTravel`), and `Topology`. Each `Tick()` call increments the tick counter, fires `TickEvent`, dispatches due packets, and raises `Ticked`. |
| `SimulationEvents.cs`             | All strongly-typed `EventArgs` used by `SimulationEngine` events: `PacketRegisteredEventArgs`, `PacketDeliveredEventArgs`, `PacketExpiredEventArgs`, `DeviceRegisteredEventArgs`, `DeviceRemovedEventArgs`, `TickedEventArgs`.                        |
| `PacketLimitExceededException.cs` | Thrown by `SimulationEngine.RegisterPacket` when the in-flight packet count reaches or exceeds `EffectivePacketLimit`. Carries `Limit`, `ActualCount`, and `AtTick`.                                                                                  |

---

## Key behaviors

- **Context-aware instance access**  -  `SimulationEngine.Instance` and `SimulationStatistics.Instance` resolve to process-global instances by default; benchmark code can push per-async-flow scoped instances to isolate parallel runs safely.
- **Packet limit guard**  -  prevents unbounded packet storms in flooding topologies. Auto-scales with device count unless `MaxActivePackets` is set manually.
- **Packet default mapping**  -  `RegisterPacket` applies `DefaultPacketTtl` and `DefaultPacketTicksToTravel` to packets that keep constructor defaults, so scenario-level TTL and travel-time settings are honored globally.
- **Pluggable strategies**  -  `Router` (type `IPacketRouter`) and `NetworkBuilder` (type `INetworkBuilder`) can be replaced at runtime without restarting the simulation. The engine default router is `SwarmProtocolPacketRouter`.

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
