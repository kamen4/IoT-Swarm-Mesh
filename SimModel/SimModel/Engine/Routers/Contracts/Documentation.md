# Engine/Routers/Contracts

This folder holds all interfaces that define the routing and topology contracts. Nothing in this folder has an implementation; implementations live in peer folders.

---

## Files

| File                         | Responsibility                                                                                                                                                   |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IPacketRouter.cs`           | Contract for a packet routing strategy. Exposes `Name` and `Route(packet, sender, topology)`. The active instance is stored on `SimulationEngine.Router`.        |
| `INetworkBuilder.cs`         | Contract for a network-formation strategy. Exposes `Name` and `Build(devices, topology)`. Called by `SimulationEngine` whenever the device registry changes.     |
| `INetworkTopology.cs`        | Read-only topology view: `GetVisibleDevices`, `GetConnectedDevices`, `AreVisible`, `AreConnected`. Passed to routers and exposed as `SimulationEngine.Topology`. |
| `IMutableNetworkTopology.cs` | Extends `INetworkTopology` with `Connect`, `Disconnect`, `RemoveDevice`, `ClearConnections`. Passed only to builders; never exposed to routers or UI.            |

---

## Extension points

Implement `IPacketRouter` to add a new routing algorithm.  
Implement `INetworkBuilder` to add a new mesh-formation strategy.  
Both can be set on `SimulationEngine` at runtime without restarting the simulation.

---

## Parent

See `Engine/Routers/Documentation.md` for the full routing subsystem description.
