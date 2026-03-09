# Engine/Routers

The `Routers` folder contains the complete routing and network-topology subsystem. It is organized into subfolders by responsibility so that contracts, topology, builders, and each router strategy are cleanly separated.

---

## Subfolders

| Subfolder        | Responsibility                                                                                                                                |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `Contracts/`     | Interfaces that define the routing and topology contracts: `IPacketRouter`, `INetworkBuilder`, `INetworkTopology`, `IMutableNetworkTopology`. |
| `Topology/`      | The concrete `NetworkTopology` implementation of `IMutableNetworkTopology`.                                                                   |
| `Builders/`      | Network-formation strategy implementations. Currently contains `FullMeshNetworkBuilder`.                                                      |
| `Flooding/`      | The `FloodingPacketRouter`  -  naive broadcast to all visible neighbours. Engine default.                                                       |
| `SmartFlooding/` | The `SmartFloodingPacketRouter`  -  direct delivery if destination visible, filtered flood otherwise. WebApp default.                           |

---

## Design principles

- **Visibility vs. Connection**  -  visibility is a Euclidean-distance relation computed on-the-fly; connection is an explicit link established by the active `INetworkBuilder`. Flooding uses visibility; unicast routing uses connections.
- **Hot-swap**  -  both `SimulationEngine.Router` and `SimulationEngine.NetworkBuilder` can be replaced at runtime to compare strategies without restarting the simulation.
- **Separation of mutation**  -  routers receive `INetworkTopology` (read-only); only builders receive `IMutableNetworkTopology`.

---

## Namespace

All files in this tree use the namespace `Engine.Routers` regardless of subfolder, preserving backward compatibility with consumers that already `using Engine.Routers;`.

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
