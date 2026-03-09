# Engine  -  IoT Swarm Mesh Simulation Engine

The **Engine** library is the self-contained simulation core of the IoT Swarm Mesh project.  
It models a wireless IoT network on a 2D plane: devices are placed at arbitrary positions, data packets flow between them tick-by-tick, and a pluggable routing algorithm propagates data toward the central hub.

The library has **no UI dependencies** and can be driven by any host (console, Blazor WebAssembly, test harness) that calls `SimulationEngine.Instance.Tick()`.

---

## Architecture overview

```mermaid
graph TD
    SE["SimulationEngine (singleton)\ntick loop * device registry\npacket priority queue * typed events\nRouter: IPacketRouter\nNetworkBuilder: INetworkBuilder\nTopology: INetworkTopology"]

    subgraph "Routing abstractions (Engine/Routers/)"
        IR["IPacketRouter\n<<interface>>\nName * Route(packet, sender, topology)"]
        IT["INetworkTopology\n<<interface>>\nGetVisibleDevices * GetConnectedDevices\nAreVisible * AreConnected"]
        IMT["IMutableNetworkTopology\n<<interface>>\nnextends INetworkTopology\nConnect * Disconnect\nRemoveDevice * ClearConnections"]
        IB["INetworkBuilder\n<<interface>>\nName * Build(devices, topology)"]

        FPR["FloodingPacketRouter\nimplements IPacketRouter\nbroadcast to all visible neighbours\n(engine default)"]
        SFR["SmartFloodingPacketRouter\nimplements IPacketRouter\ndirect if visible * filtered flood\n(WebApp default)"]
        NT["NetworkTopology\nimplements IMutableNetworkTopology\nadjacency set + on-the-fly visibility"]
        FMB["FullMeshNetworkBuilder\nimplements INetworkBuilder\nconnects every visible pair"]
    end

    subgraph Devices
        D["Device (abstract)\nidentity * position\nReceive -> SE.RoutePacket"]
        Hub["HubDevice"]
        Gen["GeneratorDevice\nSE.RoutePacket on tick"]
        Emit["EmitterDevice"]
    end

    subgraph Packets
        P["Packet"]
        PD["PacketData"]
        CP["ConfirmationPacket"]
    end

    subgraph Events["Core/SimulationEvents.cs"]
        EV["PacketRegistered * PacketDelivered\nPacketExpired * DeviceRegistered\nDeviceRemoved * Ticked"]
    end

    SE -- "owns" --> IR
    SE -- "owns" --> IB
    SE -- "owns" --> IMT
    SE -- raises --> EV

    IR -.implements.-> FPR
    IR -.implements.-> SFR
    IB -.implements.-> FMB
    IMT -.implements.-> NT
    IT -.extends.-> IMT

    SE -->|TickEvent| Gen
    SE -->|RoutePacket| IR
    IR -->|Route(packet, sender, topology)| IT
    IB -->|Build(devices, topology)| IMT
    D -->|Receive -> RoutePacket| SE

    Hub & Gen & Emit -->|extend| D
    CP -->|extends| P
    P -->|carries| PD
```

---

## Key concepts

### Tick-based time model

[`SimulationEngine`](Core/SimulationEngine.cs) is the singleton clock.  
Each call to `Tick()` represents one simulation step:

1. `TickCount` is incremented.
2. Wall-clock delta time (`dt`) is measured.
3. `TickEvent` is raised  -  subscribed devices perform periodic work (e.g. `GeneratorDevice` emitting a packet).
4. All packets whose `ArrivalTick <= TickCount` are dequeued and dispatched to their `NextHop` device.
5. `Ticked` is raised with the final tick count and `dt` for observers such as `SimulationStatistics`.

---

### Visibility vs. Connection

Two distinct spatial concepts are kept separate by design:

| Concept        | What it means                                      | Used for                                                     |
| -------------- | -------------------------------------------------- | ------------------------------------------------------------ |
| **Visibility** | Euclidean distance <= `VisibilityDistance`          | Broadcast (flooding), discovery, precondition for connection |
| **Connection** | Explicit logical link managed by `INetworkBuilder` | Unicast routing, network-topology algorithms                 |

A device can be visible to many neighbours but connected to only a subset of them (subject to per-device connection limits, protocol decisions, etc.).

`INetworkTopology` exposes both concepts:

```csharp
topology.GetVisibleDevices(device)   // all devices within radio range
topology.GetConnectedDevices(device) // explicitly connected neighbours
topology.AreVisible(a, b)
topology.AreConnected(a, b)
```

---

### Pluggable routing  -  `IPacketRouter`

[`IPacketRouter`](Routers/IPacketRouter.cs) is the single abstraction for all routing strategies.

```csharp
public interface IPacketRouter
{
    string Name { get; }
    void Route(Packet packet, Device sender, INetworkTopology topology);
}
```

The active router is stored on `SimulationEngine.Router` and can be replaced at runtime:

```csharp
SimulationEngine.Instance.Router = new MyCustomRouter();
```

Devices never reference a router directly  -  they call `SimulationEngine.Instance.RoutePacket(packet, this)`, which delegates to the currently configured `IPacketRouter`.

**Bundled implementations**

| Class                                                               | Strategy                                                                                                                                                                   |
| ------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`FloodingPacketRouter`](Routers/PacketRouter.cs)                   | **Engine default** (`SimulationEngine.Router`). Naive broadcast  -  clones the packet to every **visible** neighbour.                                                        |
| [`SmartFloodingPacketRouter`](Routers/SmartFloodingPacketRouter.cs) | **WebApp default** (`SimulationConfig.SelectedRouter`). Sends directly to destination if visible; otherwise floods all visible neighbours except `From` and `PreviousHop`. |

> **Note:** `SimulationEngine.Router` initialises to `FloodingPacketRouter`. The Blazor WebApp overrides this to `SmartFloodingPacketRouter` via `SimulationConfig.SelectedRouter`, which `SimulationService` applies on construction.

`Packet.PreviousHop` is set by `Device.Recieve` just before re-routing so the smart router knows which device the packet came from and can avoid sending it back.

---

### Pluggable network formation  -  `INetworkBuilder`

[`INetworkBuilder`](Routers/INetworkBuilder.cs) is the abstraction for mesh-formation algorithms.

```csharp
public interface INetworkBuilder
{
    string Name { get; }
    void Build(IReadOnlyList<Device> devices, IMutableNetworkTopology topology);
}
```

The active builder is stored on `SimulationEngine.NetworkBuilder` and is called automatically whenever the device registry changes.  
It can also be replaced and re-applied at runtime:

```csharp
SimulationEngine.Instance.NetworkBuilder = new MyMeshBuilder();
SimulationEngine.Instance.RebuildTopology();
```

**Bundled implementations**

| Class                                                         | Strategy                                                           |
| ------------------------------------------------------------- | ------------------------------------------------------------------ |
| [`FullMeshNetworkBuilder`](Routers/FullMeshNetworkBuilder.cs) | Connects every mutually visible pair (original implicit behaviour) |

---

### Network topology  -  `INetworkTopology` / `IMutableNetworkTopology`

[`NetworkTopology`](Routers/NetworkTopology.cs) is the live topology object owned by `SimulationEngine`.

- **Read-only view** (`INetworkTopology`) is exposed as `SimulationEngine.Topology` and passed to every `IPacketRouter.Route` call.
- **Mutable view** (`IMutableNetworkTopology`) is passed only to `INetworkBuilder.Build` so topology mutations are centralised.

Visibility is computed on-the-fly (no caching) so it always reflects current device positions.  
Connections are an in-memory adjacency set updated by the builder.

---

### Packet lifetime

A [`Packet`](Packets/Packet.cs) carries a source (`From`), destination (`To`), next-hop device, arrival tick, and a TTL counter.  
Each hop decrements TTL; a packet reaching zero is silently dropped (raises `PacketExpired`).  
If `NeedConfirmation` is set, the destination automatically routes a [`ConfirmationPacket`](Packets/ConfirmationPacket.cs) back to the originator.

**Flood-clone identity fields**

Because flooding broadcasts cloned copies of a packet to every visible neighbour, two extra fields are stamped on `Packet` to make statistical analysis of these copies possible:

| Field        | Set when                                             | Preserved by `Clone()` | Purpose                                                  |
| ------------ | ---------------------------------------------------- | ---------------------- | -------------------------------------------------------- |
| `OriginId`   | Constructor  -  equals `Id` on original                | (yes) (MemberwiseClone)    | Groups all clones of one logical message                 |
| `InitialTtl` | First `RegisterPacket` call (when `InitialTtl == 0`) | (yes)                      | Enables hop-count calculation: `hops = InitialTtl - TTL` |

These fields drive the two new statistics metrics (see below).

---

### Active-packet limit

`SimulationEngine` enforces a configurable upper bound on the number of simultaneously in-flight packets to prevent packet storms (e.g. routing loops in flooding networks) from growing unbounded.

| Property                       | Default          | Meaning                                                   |
| ------------------------------ | ---------------- | --------------------------------------------------------- |
| `MaxActivePackets`             | `0`              | `0` = automatic limit                                     |
| `EffectivePacketLimit`         | `devices x 2000` | The limit actually enforced on each `RegisterPacket` call |
| `AUTO_PACKET_LIMIT_PER_DEVICE` | `2000`           | Multiplier for the automatic calculation                  |

**Automatic mode (`MaxActivePackets = 0`):**  
The limit is recomputed on every `RegisterPacket` call as `Devices.Count x AUTO_PACKET_LIMIT_PER_DEVICE`.  
This means the limit automatically scales up as more devices are added to the simulation.

**Manual override:**  
Set `MaxActivePackets` to any positive integer to fix the limit regardless of device count.  
The UI settings panel exposes this value and shows the current automatic limit as a hint.

**Violation behaviour:**  
When `_packets.Count >= EffectivePacketLimit`, `RegisterPacket` throws [`PacketLimitExceededException`](Core/PacketLimitExceededException.cs) containing the limit, actual count, and the tick at which the violation occurred.  
`SimulationService` in the Blazor client catches this exception, stops the tick loop, and stores the message in `PacketLimitError` which is rendered as a red error banner.

```
active packets  limit not reached -> packet enqueued normally
active packets >= limit            -> PacketLimitExceededException thrown
                                    -> RunLoopAsync catches it
                                    -> IsRunning = false, PacketLimitError set
                                    -> error banner shown in UI
```

---

## Project structure

| Path                                                                                                       | Responsibility                                                                                                                         |
| ---------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| [`Core/SimulationEngine.cs`](Core/SimulationEngine.cs)                                                     | Singleton engine: tick loop, device registry, packet queue, typed events, `Router` + `NetworkBuilder` + `Topology`, packet-limit guard |
| [`Core/SimulationEvents.cs`](Core/SimulationEvents.cs)                                                     | Strongly-typed `EventArgs` for all engine events                                                                                       |
| [`Core/PacketLimitExceededException.cs`](Core/PacketLimitExceededException.cs)                             | Exception thrown when the in-flight packet count exceeds `MaxActivePackets`                                                            |
| [`Devices/Device.cs`](Devices/Device.cs)                                                                   | Abstract base: identity, 2D position, receive / forward via `SimulationEngine.RoutePacket`                                             |
| [`Devices/HubDevice.cs`](Devices/HubDevice.cs)                                                             | Central gateway; destination for all `GeneratorDevice` packets                                                                         |
| [`Devices/GeneratorDevice.cs`](Devices/GeneratorDevice.cs)                                                 | Emits one packet per `GenFrequencyTicks` ticks via `RoutePacket`                                                                       |
| [`Devices/EmitterDevice.cs`](Devices/EmitterDevice.cs)                                                     | Receives `ControlPacket` and toggles boolean state; Hub drives it at `ControlFrequencyTicks`                                           |
| [`Packets/Packet.cs`](Packets/Packet.cs)                                                                   | Core transmission unit: routing metadata, TTL, `OriginId`, `InitialTtl`, payload                                                       |
| [`Packets/PacketData.cs`](Packets/PacketData.cs)                                                           | Untyped application-level payload wrapper                                                                                              |
| [`Packets/ConfirmationPacket.cs`](Packets/ConfirmationPacket.cs)                                           | Delivery acknowledgement routed back to the originator                                                                                 |
| [`Packets/ControlPacket.cs`](Packets/ControlPacket.cs)                                                     | Hub->Emitter command packet carrying a `bool Command` (on/off)                                                                          |
| [`Routers/Contracts/IPacketRouter.cs`](Routers/Contracts/IPacketRouter.cs)                                 | **<<interface>>** routing strategy contract                                                                                              |
| [`Routers/Contracts/INetworkTopology.cs`](Routers/Contracts/INetworkTopology.cs)                           | **<<interface>>** read-only topology: visibility + connections                                                                           |
| [`Routers/Contracts/IMutableNetworkTopology.cs`](Routers/Contracts/IMutableNetworkTopology.cs)             | **<<interface>>** topology mutations (builder-only)                                                                                      |
| [`Routers/Contracts/INetworkBuilder.cs`](Routers/Contracts/INetworkBuilder.cs)                             | **<<interface>>** network-formation strategy contract                                                                                    |
| [`Routers/Topology/NetworkTopology.cs`](Routers/Topology/NetworkTopology.cs)                               | Concrete topology: on-the-fly visibility + adjacency-set connections                                                                   |
| [`Routers/Builders/FullMeshNetworkBuilder.cs`](Routers/Builders/FullMeshNetworkBuilder.cs)                 | `FullMeshNetworkBuilder`  -  connect every visible pair                                                                                  |
| [`Routers/Flooding/PacketRouter.cs`](Routers/Flooding/PacketRouter.cs)                                     | `FloodingPacketRouter`  -  broadcast to all visible neighbours (engine default)                                                          |
| [`Routers/SmartFlooding/SmartFloodingPacketRouter.cs`](Routers/SmartFlooding/SmartFloodingPacketRouter.cs) | `SmartFloodingPacketRouter`  -  direct if destination visible; filtered flood otherwise (WebApp default)                                 |
| [`Statistics/SimulationStatistics.cs`](Statistics/SimulationStatistics.cs)                                 | Event-driven singleton: ten metrics + history ring-buffer                                                                              |
| [`Statistics/StatMetric.cs`](Statistics/StatMetric.cs)                                                     | Single observable metric with display formatting and plottable flag                                                                    |
| [`Statistics/TickSnapshot.cs`](Statistics/TickSnapshot.cs)                                                 | Immutable per-tick value record for time-series charting (8 plottable fields)                                                          |
| [`Benchmark/BenchmarkConfig.cs`](Benchmark/BenchmarkConfig.cs)                                             | Fully serialisable scenario: devices + events + settings + router list                                                                 |
| [`Benchmark/BenchmarkEventEntry.cs`](Benchmark/BenchmarkEventEntry.cs)                                     | Tick-stamped event union: Toggle / RemoveDevice / AddDevice                                                                            |
| [`Benchmark/DeviceBenchmarkDto.cs`](Benchmark/DeviceBenchmarkDto.cs)                                       | Serialisable device description (record, supports `with`)                                                                              |
| [`Benchmark/BenchmarkResult.cs`](Benchmark/BenchmarkResult.cs)                                             | Per-router final metrics + full `TickSnapshot[]` history                                                                               |
| [`Benchmark/BenchmarkSession.cs`](Benchmark/BenchmarkSession.cs)                                           | Root JSON document: config + list of results, self-contained for sharing                                                               |
| [`Benchmark/BenchmarkRunner.cs`](Benchmark/BenchmarkRunner.cs)                                             | Headless tick loop: resets engine, applies events, runs N routers, returns session                                                     |

---

## Comparing protocols on the same network

Because both `Router` and `NetworkBuilder` are hot-swappable properties on the singleton engine, you can:

- Run a fixed network for N ticks with protocol A -> collect `SimulationStatistics`.
- Call `SimulationEngine.Instance.Router = protocolB; SimulationEngine.Instance.Reset()` -> run again.
- Compare the two `SimulationStatistics` snapshots side-by-side in the UI.

The **Benchmark subsystem** (`Engine/Benchmark/`) automates this workflow end-to-end:

```
BenchmarkConfig          <- user defines: devices, events, settings, router names
    |
    v
BenchmarkRunner.Run()    <- headless loop; one full reset+run per router
    |  for each router:
    |    1. engine.Reset() + stats.Reset()
    |    2. apply engine settings + register initial devices
    |    3. tick loop (0..DurationTicks):
    |         - fire scheduled BenchmarkEvents at matching ticks
    |         - engine.Tick()
    |    4. capture BenchmarkResult (final metrics + TickSnapshot[])
    |
    v
BenchmarkSession         <- { Config, Results[] }  -  JSON-serialisable
```

**Scheduled events** (`BenchmarkEventEntry`) supported:

| Type                                     | Effect                                                     |
| ---------------------------------------- | ---------------------------------------------------------- |
| `ToggleBenchmarkEvent(deviceName)`       | Hub sends a `ControlPacket` toggle to the named emitter    |
| `RemoveDeviceBenchmarkEvent(deviceName)` | Device is removed from the engine (simulates node failure) |
| `AddDeviceBenchmarkEvent(dto)`           | New device is registered (simulates node joining)          |

**Save / Load:** `BenchmarkSession` serialises to compact JSON via `System.Text.Json` with polymorphic `$type` discriminators on `BenchmarkEvent`. The Blazor `BenchmarkService` exposes `Serialize` / `Deserialize` and triggers a browser file download via `downloadTextFile` JS interop.

---

## Documentation hierarchy

Each subfolder contains its own `Documentation.md`:

- `Core/Documentation.md`  -  engine singleton, events, packet-limit exception.
- `Devices/Documentation.md`  -  device base class and all concrete device types.
- `Packets/Documentation.md`  -  packet types and payload.
- `Routers/Documentation.md`  -  routing subsystem overview and design principles.
- `Routers/Contracts/Documentation.md`  -  all routing and topology interfaces.
- `Routers/Topology/Documentation.md`  -  `NetworkTopology` implementation.
- `Routers/Builders/Documentation.md`  -  network-formation strategies.
- `Routers/Flooding/Documentation.md`  -  `FloodingPacketRouter`.
- `Routers/SmartFlooding/Documentation.md`  -  `SmartFloodingPacketRouter`.
- `Statistics/Documentation.md`  -  metrics, observable values, history ring buffer.
- `Benchmark/Documentation.md`  -  headless benchmark runner and session types.
