# WebApp/WebApp.Client/Services

Contains all client-side services and view-models consumed by the UI layer. No Engine types are referenced directly from components  -  components go through these services instead.

---

## Files

| File                   | Responsibility                                                                                                                                                                                                                                                                                                                                                                   |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SimulationService.cs` | Primary UI service. Owns the `PeriodicTimer`-based async tick loop (`RunLoopAsync`). Exposes `Start`, `Stop`, `Reset`, `AddDevice`, `UpdateDevice`, `RemoveDevice`, `LoadPreset`, `GenerateRandom`, and `ApplyConfig`. Raises `StateChanged` after every tick and state-changing operation. Catches `PacketLimitExceededException` and stores the message in `PacketLimitError`. |
| `SimulationConfig.cs`  | Flat settings model bound to `SimulationSettings.razor`. Maps UI fields (`TickIntervalMs`, `DefaultTTL`, `TicksToTravel`, `VisibilityDistance`, `MaxActivePackets`, `SelectedRouter`) to engine properties via `SimulationService.ApplyConfig()`.                                                                                                                                |
| `BenchmarkService.cs`  | Wraps `BenchmarkRunner` for async WASM execution. Manages a `Progress` fraction, a `LastSession` result, and an in-memory `SavedSessions` library. Provides `Serialize` / `Deserialize` for JSON save/load via browser file interop.                                                                                                                                             |
| `SimulationPreset.cs`  | Immutable record `(Name, Description, Build)` where `Build` is an `Action<SimulationService>` that registers devices into a freshly reset engine.                                                                                                                                                                                                                                |
| `SimulationPresets.cs` | Static catalog of ten built-in presets (Default, Line, Star, Dense Cluster, Two Clusters, Three Clusters, Grid 3x3, Long Chain, Random small, Random large).                                                                                                                                                                                                                     |
| `DeviceFormModel.cs`   | View-model for the Add / Edit Device modal. Holds `Name`, `DeviceType`, `X`, `Y`, `GenFrequencyTicks`, and `ControlFrequencyTicks`.                                                                                                                                                                                                                                              |

---

## Registration

`SimulationService` and `BenchmarkService` are registered as scoped dependencies in `Program.cs` and injected into `Home.razor` via `@inject`.

---

## Parent

See `WebApp/WebApp.Client/Documentation.md` for the overall client project structure.  
See `WebApp/Documentation.md` for detailed service API tables.
