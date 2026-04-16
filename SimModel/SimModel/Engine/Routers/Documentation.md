# Engine/Routers

The `Routers` folder contains the complete routing and network-topology subsystem. It is organized into subfolders by responsibility so that contracts, topology, builders, and each router strategy are cleanly separated.

It also contains shared router-level policies used by all strategies.

---

## Subfolders

| Subfolder | Responsibility |
| --- | --- |
| `Contracts/` | Interfaces that define the routing and topology contracts: `IPacketRouter`, `INetworkBuilder`, `INetworkTopology`, `IMutableNetworkTopology`. |
| `Topology/` | The concrete `NetworkTopology` implementation of `IMutableNetworkTopology`. |
| `Builders/` | Network-formation strategy implementations. Currently contains `FullMeshNetworkBuilder`. |
| `Flooding/` | The `FloodingPacketRouter` - naive broadcast to all visible neighbours. Useful baseline strategy for comparison. |
| `SmartFlooding/` | The `SmartFloodingPacketRouter` - direct delivery if destination visible, filtered flood otherwise. Reduced clone fan-out alternative to naive flooding. |
| `SwarmProtocol/` | The `SwarmProtocolPacketRouter` - protocol-oriented direction-aware routing: UP best-neighbor by charge and DOWN tree-first forwarding. |

## Files

| File | Responsibility |
| --- | --- |
| `RoutingNeighborPolicy.cs` | Shared deterministic visibility policy for all routers. Selects nearest visible top-k neighbors (default k=10) to keep fairness and comparable fan-out across routing strategies. |

---

## Design principles

- **Visibility vs. Connection**  -  visibility is a Euclidean-distance relation computed on-the-fly; connection is an explicit link established by the active `INetworkBuilder`. All bundled routers first apply the same nearest-visible top-k filter (`k=10`) for fair comparison.
- **Direction-aware routing** - protocol routers can branch behavior by `Packet.Direction` and `Packet.MessageType` while still using the same `IPacketRouter` contract.
- **Hot-swap**  -  both `SimulationEngine.Router` and `SimulationEngine.NetworkBuilder` can be replaced at runtime to compare strategies without restarting the simulation.
- **Separation of mutation**  -  routers receive `INetworkTopology` (read-only); only builders receive `IMutableNetworkTopology`.

---

## Namespace

All files in this tree use the namespace `Engine.Routers` regardless of subfolder, preserving backward compatibility with consumers that already `using Engine.Routers;`.

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
