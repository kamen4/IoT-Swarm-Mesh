# IoT Swarm Mesh - Engine Documentation

Welcome to the **IoT Swarm Mesh** simulation engine documentation.

This site is generated from source XML comments and hand-written conceptual guides
using [DocFX](https://dotnet.github.io/docfx/).

The **Engine** library is the self-contained simulation core of the IoT Swarm Mesh
project. It models a wireless IoT network on a 2D plane: devices are placed at
arbitrary positions, data packets flow between them tick-by-tick, and a pluggable
routing algorithm propagates data toward the central hub.

No UI dependencies - any host that calls `SimulationEngine.Instance.Tick()` can
drive the simulation (console, Blazor WASM, test harness).

> Source: [github.com/kamen4/IoT-Swarm-Mesh](https://github.com/kamen4/IoT-Swarm-Mesh)

---

## Conceptual documentation

| Section                                                            | Description                                                      |
| ------------------------------------------------------------------ | ---------------------------------------------------------------- |
| [Engine](Documentation.md)                                         | Architecture overview, key concepts, tick model, packet lifetime |
| [Core](Core/Documentation.md)                                      | `SimulationEngine` singleton, events, packet-limit guard         |
| [Devices](Devices/Documentation.md)                                | Device base class and all concrete types                         |
| [Packets](Packets/Documentation.md)                                | Packet types, payload, TTL, flood-clone identity                 |
| [Routers](Routers/Documentation.md)                                | Routing subsystem overview and design principles                 |
| [Routers / Contracts](Routers/Contracts/Documentation.md)          | `IPacketRouter`, `INetworkBuilder`, topology interfaces          |
| [Routers / Topology](Routers/Topology/Documentation.md)            | `NetworkTopology` - adjacency set + on-the-fly visibility        |
| [Routers / Builders](Routers/Builders/Documentation.md)            | `FullMeshNetworkBuilder` and extension points                    |
| [Routers / Flooding](Routers/Flooding/Documentation.md)            | `FloodingPacketRouter` - engine default                          |
| [Routers / Smart Flooding](Routers/SmartFlooding/Documentation.md) | `SmartFloodingPacketRouter` - WebApp default                     |
| [Statistics](Statistics/Documentation.md)                          | Ten observable metrics + history ring buffer                     |
| [Benchmark](Benchmark/Documentation.md)                            | Headless multi-router comparison runner                          |

---

## API reference

Auto-generated from XML `<summary>` comments on every public type and member.

- [Browse API Reference](api/index.md)
