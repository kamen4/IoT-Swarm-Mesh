# WebApp  -  IoT Swarm Mesh Presentation Layer

The **WebApp** project is the interactive front end for the IoT Swarm Mesh simulation.
It is a Blazor WebAssembly application hosted as a standalone static site, consuming the
`Engine` library directly in the browser via .NET WASM.

The app has **no server component** for the simulation itself  -  all tick processing runs
client-side inside the browser.

---

## Architecture overview

```
Browser (WASM)
|
+-- Home.razor ---------------------------- single-page application shell
|    |
|    +-- SimulationService ----------------- tick loop, device CRUD, presets
|    |    +-- SimulationEngine (singleton)  <- Engine library
|    |    +-- SimulationStatistics          <- Engine library
|    |    +-- SimulationConfig              <- UI settings model
|    |
|    +-- BenchmarkService ------------------ headless multi-router comparison
|    |    +-- BenchmarkRunner               <- Engine library
|    |
|    +-- Shared components
|         +-- NetworkGraph.razor
|         +-- DeviceTable.razor / DeviceModal.razor
|         +-- PacketTable.razor
|         +-- StatisticsPanel.razor / DetailedStatsModal.razor
|         +-- SimulationSettings.razor
|         +-- RandomGenerationModal.razor
|         +-- BenchmarkConfigModal.razor
|         +-- BenchmarkResultModal.razor
|         +-- BenchmarkLibraryModal.razor
```

---

## Services (`WebApp.Client/Services/`)

### `SimulationService`

Drives the simulation from the UI.  
Owns a `PeriodicTimer`-based async tick loop (`RunLoopAsync`) and exposes
high-level operations consumed by `Home.razor`:

| Method / property        | Purpose                                                                               |
| ------------------------ | ------------------------------------------------------------------------------------- |
| `Start()` / `Stop()`     | Start or cancel the `PeriodicTimer` tick loop                                         |
| `ApplyConfig()`          | Push `SimulationConfig` settings into `SimulationEngine`; restarts if already running |
| `AddDevice(form)`        | Create a device from `DeviceFormModel` and register it with the engine                |
| `UpdateDevice(id, form)` | Mutate an existing device's name, position, and frequency                             |
| `RemoveDevice(id)`       | Delegate to `SimulationEngine.RemoveDevice`                                           |
| `LoadPreset(preset)`     | Reset engine and apply a `SimulationPreset` layout; remembered for `Reset()`          |
| `GenerateRandom(...)`    | Reset engine and generate a random layout (including connected-topology mode) with configurable device and frequency ranges |
| `Reset()`                | Restore the last-loaded preset, or the built-in default layout                        |
| `PacketLimitError`       | Non-null when the tick loop stopped due to `PacketLimitExceededException`             |
| `ActivePresetName`       | Name shown on the Reset button tooltip                                                |
| `StateChanged`           | Event raised after every tick or state change  -  components subscribe for re-renders   |

**Default router:** `SimulationConfig.SelectedRouter` defaults to
`SwarmProtocolPacketRouter`, which `SimulationService` pushes to `SimulationEngine.Router`
in its constructor. Users can switch to `SmartFloodingPacketRouter` or
`FloodingPacketRouter` from settings.

---

### `SimulationConfig`

Flat settings model bound to `SimulationSettings.razor`.

| Property             | Default                     | Engine mapping                                 |
| -------------------- | --------------------------- | ---------------------------------------------- |
| `TickIntervalMs`     | `300`                       | delay between `PeriodicTimer` ticks            |
| `DefaultTTL`         | `10`                        | applied when creating packets                  |
| `TicksToTravel`      | `3`                         | applied when creating packets                  |
| `VisibilityDistance` | `200`                       | `SimulationEngine.VisibilityDistance`          |
| `MaxActivePackets`   | `0`                         | `SimulationEngine.MaxActivePackets` (0 = auto) |
| `SelectedRouter`     | `SwarmProtocolPacketRouter` | `SimulationEngine.Router`                      |
| `AvailableRouters`   | `[SwarmProtocol, Smart, Flooding]` | bound to the router dropdown           |

---

### `BenchmarkService`

Runs headless multi-router benchmarks asynchronously without blocking the WASM UI thread.

| Member                                                  | Purpose                                                                |
| ------------------------------------------------------- | ---------------------------------------------------------------------- |
| `RunAsync(config)`                                      | Starts `BenchmarkRunner.Run` on a thread-pool thread; reports progress |
| `Progress`                                              | `[0, 1]` overall fraction updated continuously during each router run  |
| `CurrentRouterName` / `CurrentRouterIndex`              | Which router is currently executing                                    |
| `CurrentTick` / `DurationTicks`                         | Live per-router tick progress                                          |
| `CurrentRouterProgress` / `Elapsed`                     | Per-router percentage and elapsed benchmark runtime                    |
| `LastSession`                                           | Most recent `BenchmarkSession` result                                  |
| `SavedSessions`                                         | In-memory library of saved sessions (lost on page refresh)             |
| `SaveToLibrary(session)` / `DeleteFromLibrary(session)` | Manage the in-memory library                                           |
| `Serialize(session)`                                    | Produce JSON for browser download                                      |
| `Deserialize(json)`                                     | Parse JSON from a user-uploaded file; returns `null` on error          |
| `AvailableRouters`                                      | Router registry keyed by `IPacketRouter.Name` (used by the runner)     |

---

### `SimulationPreset` / `SimulationPresets`

`SimulationPreset` is an immutable record: `(Name, Description, Build)`.  
`Build` is an `Action<SimulationService>` that registers devices into a freshly-reset engine.

`SimulationPresets.All` contains ten built-in presets:

| Preset         | Layout                                            |
| -------------- | ------------------------------------------------- |
| Default        | Hub + 3 sensors + 2 lamps                         |
| Line           | Hub + 5 nodes in a chain                          |
| Star           | Hub + 8 nodes on a ring                           |
| Dense Cluster  | Hub + 10 nodes, every node visible to every other |
| Two Clusters   | Two groups bridged by one relay node              |
| Three Clusters | Three groups bridged by relay nodes on each edge  |
| Grid 3x3       | 3x3 grid with cross-neighbour connectivity only   |
| Long Chain     | Hub + 8 nodes in the longest possible chain       |
| Random (small) | 8 randomly placed devices, seed 1                 |
| Random (large) | 20 randomly placed devices, seed 2                |

---

### `DeviceFormModel`

View-model for the Add / Edit Device modal (bound by `DeviceModal.razor`).

| Property                | Relevant device type           |
| ----------------------- | ------------------------------ |
| `Name`                  | all                            |
| `DeviceType`            | `Hub \| Generator \| Emitter`  |
| `X`, `Y`                | all                            |
| `GenFrequencyTicks`     | `GeneratorDevice`              |
| `ControlFrequencyTicks` | `EmitterDevice` (0 = disabled) |

---

## Shared components (`WebApp.Client/Shared/`)

| Component                     | Role                                                                                                                                  |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `NetworkGraph.razor`          | SVG canvas showing devices as circles, connections as lines, and packets as animated dots. Scales to the simulation coordinate space. |
| `DeviceTable.razor`           | Shows all registered devices with type, position, and frequency; host for Add / Edit / Delete actions.                                |
| `DeviceModal.razor`           | Add / Edit modal bound to `DeviceFormModel`; validates and calls back `SimulationService`.                                            |
| `PacketTable.razor`           | Live table of in-flight packets with protocol fields (message type, direction, MAC addresses, charge) plus source, destination, next-hop, and TTL. |
| `StatisticsPanel.razor`       | Summary card grid showing all `SimulationStatistics.Metrics`; includes a time-series chart of plottable metrics.                      |
| `DetailedStatsModal.razor`    | Full-screen modal with an expanded chart and per-tick history table.                                                                  |
| `SimulationSettings.razor`    | Settings modal bound to `SimulationConfig`; exposes tick interval, TTL, travel time, visibility, packet limit, and router selection.  |
| `RandomGenerationModal.razor` | Modal for random simulation generation: connected graph parameters, generator/emitter share, and frequency ranges.                    |
| `BenchmarkConfigModal.razor`  | Modal for composing a `BenchmarkConfig`: device layout, events, settings, router list, and one-click full random scenario generation. |
| `BenchmarkResultModal.razor`  | Shows a `BenchmarkSession`'s per-router metrics and time-series charts side-by-side.                                                  |
| `BenchmarkLibraryModal.razor` | In-session library of saved `BenchmarkSession` objects; supports load, delete, and JSON download/upload.                              |

---

## Page: `Home.razor`

The single page of the application.

**Top bar**  -  always visible:

- Tick counter, device count, active-packet counter with limit, and last-tick `dt`
- Start / Stop button
- Reset button (tooltip shows the active preset name)
- Preset selector + Load button
- Random button (opens dedicated random-generation modal)
- Settings, Stats, Benchmark, Library, and Docs buttons

**Error banner**  -  shown when `SimulationService.PacketLimitError` is set.

**Main grid** (two-column):

- Left column: `DeviceTable` + `PacketTable`
- Right column: `NetworkGraph`

**Stats panel**  -  collapsible overlay showing `StatisticsPanel`.

**Benchmark running overlay**  -  displays detailed benchmark runtime telemetry: current router, router index/total, tick progress, router/overall percentages, and elapsed time.

---

## Project structure

| Path                                               | Responsibility                                                                |
| -------------------------------------------------- | ----------------------------------------------------------------------------- |
| `WebApp.Client/Pages/Home.razor`                   | Single-page application shell; top bar, main grid, modal wiring               |
| `WebApp.Client/Services/SimulationService.cs`      | Tick loop, device CRUD, preset / random management                            |
| `WebApp.Client/Services/RandomGenerationOptions.cs`| Random layout options model consumed by `SimulationService` and random modal   |
| `WebApp.Client/Services/SimulationConfig.cs`       | Flat settings model for the UI settings panel                                 |
| `WebApp.Client/Services/BenchmarkService.cs`       | Async benchmark runner wrapper, session library, JSON serialisation           |
| `WebApp.Client/Services/SimulationPreset.cs`       | Preset record type                                                            |
| `WebApp.Client/Services/SimulationPresets.cs`      | Catalog of ten built-in presets                                               |
| `WebApp.Client/Services/DeviceFormModel.cs`        | View-model for Add / Edit Device modal                                        |
| `WebApp.Client/Shared/NetworkGraph.razor`          | SVG network visualisation                                                     |
| `WebApp.Client/Shared/DeviceTable.razor`           | Device list with add / edit / delete                                          |
| `WebApp.Client/Shared/DeviceModal.razor`           | Add / Edit device modal                                                       |
| `WebApp.Client/Shared/PacketTable.razor`           | In-flight packet list                                                         |
| `WebApp.Client/Shared/StatisticsPanel.razor`       | Metric cards + time-series chart                                              |
| `WebApp.Client/Shared/DetailedStatsModal.razor`    | Expanded statistics modal                                                     |
| `WebApp.Client/Shared/SimulationSettings.razor`    | Settings modal                                                                |
| `WebApp.Client/Shared/RandomGenerationModal.razor` | Random layout generation modal                                                |
| `WebApp.Client/Shared/BenchmarkConfigModal.razor`  | Benchmark scenario composer                                                   |
| `WebApp.Client/Shared/BenchmarkResultModal.razor`  | Benchmark results viewer                                                      |
| `WebApp.Client/Shared/BenchmarkLibraryModal.razor` | Saved sessions library                                                        |
| `WebApp.Client/Program.cs`                         | DI registration: `SimulationService`, `BenchmarkService` as scoped singletons |

---

## Documentation hierarchy

Each subfolder under `WebApp.Client/` contains its own `Documentation.md`:

- `WebApp.Client/Documentation.md`  -  client project overview, entry point, DI setup.
- `WebApp.Client/Pages/Documentation.md`  -  `Home.razor` responsibilities.
- `WebApp.Client/Services/Documentation.md`  -  all service and model files.
- `WebApp.Client/Shared/Documentation.md`  -  all shared component files.
- `WebApp.Client/Layout/Documentation.md`  -  layout components.

---

## Packet-limit error flow

```
SimulationService.RunLoopAsync
    +-- Engine.Tick()
         +-- RegisterPacket() -> PacketLimitExceededException
              +-- caught in RunLoopAsync:
                   IsRunning = false
                   PacketLimitError = ex.Message
                   StateChanged raised
                        +-- Home.razor renders error banner
                             +-- user clicks Reset -> PacketLimitError cleared
```
