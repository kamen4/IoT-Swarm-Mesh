# Engine/Routers/Topology

This folder contains the concrete implementation of the network topology used by the engine at runtime.

---

## Files

| File                 | Responsibility                                                                                                                                                                                                                                                                                                                                        |
| -------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `NetworkTopology.cs` | Implements `IMutableNetworkTopology`. Maintains an in-memory adjacency set of connections (bidirectional, GUIDs). Visibility is computed on-the-fly from device positions and `SimulationEngine.VisibilityDistance`  -  it is never cached. Owned exclusively by `SimulationEngine`; exposed as `INetworkTopology` through `SimulationEngine.Topology`. |

---

## Key behaviors

- Visibility queries are O(n) over all registered devices.
- Connection queries are O(1) via hash-set lookup.
- `ClearConnections` is called by every `INetworkBuilder.Build` invocation before rewriting the graph.
- `RemoveDevice` cleans up all edges incident to the removed device to prevent stale references.

---

## Parent

See `Engine/Routers/Documentation.md` for the full routing subsystem description.
