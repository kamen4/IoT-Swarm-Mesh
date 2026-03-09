# Engine/Routers/Builders

This folder contains network-formation strategy implementations (classes that implement `INetworkBuilder`). Each builder decides which pairs of devices become connected after a topology rebuild.

---

## Files

| File                        | Responsibility                                                                                                                                                                                                                         |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `FullMeshNetworkBuilder.cs` | Connects every pair of mutually visible devices, producing a full-visibility mesh. Edges grow as O(n^2). This mirrors the original implicit behaviour of the engine. Use a limited-degree or spanning-tree builder for larger networks. |

---

## Extension points

Add new builders by implementing `INetworkBuilder` from `Engine/Routers/Contracts/`. Set `SimulationEngine.Instance.NetworkBuilder` at runtime to switch strategies without restarting. Call `SimulationEngine.Instance.RebuildTopology()` to apply the new builder to the current device set.

---

## Parent

See `Engine/Routers/Documentation.md` for the full routing subsystem description.
