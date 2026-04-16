# SimModel  -  IoT Swarm Mesh Simulation

SimModel is a three-project .NET 9 solution that models a wireless IoT mesh network.  
The **Engine** library contains the entire simulation core with no UI dependencies.  
The **WebApp** project is a Blazor WebAssembly application that runs in the browser and hosts the Engine, providing a visual interface for running, inspecting, and benchmarking simulations.
The **ConsoleUI** project is a terminal benchmark runner that executes realistic scenarios, supports interactive case generation, emits per-scenario session JSON files, emits one combined HTML report per command, and can schedule runs across multiple CPU cores in CLI mode.

---

## Solution structure

| Folder                  | Project                | Responsibility                                                                             |
| ----------------------- | ---------------------- | ------------------------------------------------------------------------------------------ |
| `Engine/`               | `Engine.csproj`        | Self-contained simulation engine: devices, packets, routing, statistics, benchmark runner. |
| `ConsoleUI/`            | `ConsoleUI.csproj`     | Console benchmark app: CLI and interactive run modes, realistic preset and generated scenarios, live progress, JSON/HTML report export. |
| `WebApp/`               | Folder grouping        | Contains the Blazor WASM host and client-side project.                                     |
| `WebApp/WebApp.Client/` | `WebApp.Client.csproj` | Blazor WASM application: UI pages, shared components, client services.                     |

---

## Key interactions

- `WebApp.Client` references `Engine` directly; the Engine singletons (`SimulationEngine`, `SimulationStatistics`) are shared across the entire WASM session.
- `ConsoleUI` references `Engine` directly and runs `BenchmarkRunner` headlessly with deterministic scenario presets or generated cases, then exports per-scenario JSON plus one combined HTML artifact.
- CLI benchmark runs support bounded worker parallelism via `--parallelism`; multi-scenario sweeps run concurrently, and single-scenario runs can execute router benchmarks in parallel.
- The simulation tick loop runs inside `SimulationService` using `PeriodicTimer`; it calls `SimulationEngine.Instance.Tick()` on each interval.
- Routing strategy and network-formation strategy are swappable at runtime: the UI writes to `SimulationEngine.Router` / `SimulationEngine.NetworkBuilder`.
- `SimulationEngine.Instance` and `SimulationStatistics.Instance` resolve to global instances by default, while benchmark code can push scoped instances to isolate parallel runs safely.
- The default router in both Engine and WebApp is now `SwarmProtocolPacketRouter`, which applies charge-based UP forwarding and tree-first DOWN forwarding.
- All bundled router strategies apply the same nearest-visible top-k neighbor window (`k=10`) before routing decisions for symmetric benchmark fairness.

---

## Documentation hierarchy

Read `Engine/Documentation.md` for engine architecture and structure.  
Read `ConsoleUI/Documentation.md` for the console benchmark runner structure and report flow.  
Read `WebApp/Documentation.md` for the WebApp structure and UI component responsibilities.  
Each subfolder contains its own `Documentation.md` for folder-level detail.
