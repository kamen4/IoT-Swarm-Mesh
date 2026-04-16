# WebApp/WebApp.Client/Shared

Contains all reusable Blazor components. Each component is a self-contained UI unit with no direct Engine references  -  it communicates through `SimulationService` and `BenchmarkService` injected via DI.

---

## Files

| File                          | Responsibility                                                                                                                                                                                     |
| ----------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `NetworkGraph.razor`          | SVG canvas. Renders devices as labeled circles, established connections as grey lines, and in-flight packets as animated colored dots. Scales the simulation coordinate space to fit the viewport. |
| `DeviceTable.razor`           | Table of all registered devices (type, name, position, frequency). Hosts the Add button and per-row Edit and Delete actions.                                                                       |
| `DeviceModal.razor`           | Add / Edit device modal. Bound to `DeviceFormModel`; validates inputs and delegates to `SimulationService.AddDevice` or `UpdateDevice`.                                                            |
| `PacketTable.razor`           | Live table of packets currently in-flight, showing protocol fields (`MessageType`, `Direction`, origin/destination MAC, advertised charge) plus source/destination devices, next hop, and TTL. |
| `StatisticsPanel.razor`       | Summary grid of all ten `SimulationStatistics.Metrics` as card tiles, plus a time-series line chart of the plottable metrics.                                                                      |
| `DetailedStatsModal.razor`    | Full-screen modal with an expanded chart and a scrollable per-tick history table derived from `SimulationStatistics.History`.                                                                      |
| `SimulationSettings.razor`    | Settings modal. Bound to `SimulationConfig`; exposes tick interval, TTL, travel time, visibility distance, packet limit, and router dropdown.                                                      |
| `RandomGenerationModal.razor` | Dedicated random layout modal. Collects generation parameters (counts, ratios, frequency ranges) and calls `SimulationService.GenerateRandom` with connected topology defaults.                  |
| `BenchmarkConfigModal.razor`  | Benchmark scenario composer. Allows adding devices, scheduling events, setting duration and engine config, choosing routers to compare, and generating a full random benchmark scenario (devices, events, settings, routers).                                                        |
| `BenchmarkResultModal.razor`  | Results viewer. Shows per-router final metrics and overlaid time-series charts for side-by-side comparison.                                                                                        |
| `BenchmarkLibraryModal.razor` | In-session library of saved `BenchmarkSession` objects. Supports load, delete, JSON download, and JSON upload.                                                                                     |

---

## Parent

See `WebApp/WebApp.Client/Documentation.md` for the overall client project structure.  
See `WebApp/Documentation.md` for a description of how each component is wired into `Home.razor`.
