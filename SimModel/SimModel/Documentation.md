# SimModel  -  IoT Swarm Mesh Simulation

SimModel is a two-project .NET 9 solution that models a wireless IoT mesh network.  
The **Engine** library contains the entire simulation core with no UI dependencies.  
The **WebApp** project is a Blazor WebAssembly application that runs in the browser and hosts the Engine, providing a visual interface for running, inspecting, and benchmarking simulations.

---

## Solution structure

| Folder                  | Project                | Responsibility                                                                             |
| ----------------------- | ---------------------- | ------------------------------------------------------------------------------------------ |
| `Engine/`               | `Engine.csproj`        | Self-contained simulation engine: devices, packets, routing, statistics, benchmark runner. |
| `WebApp/`               | Folder grouping        | Contains the Blazor WASM host and client-side project.                                     |
| `WebApp/WebApp.Client/` | `WebApp.Client.csproj` | Blazor WASM application: UI pages, shared components, client services.                     |

---

## Key interactions

- `WebApp.Client` references `Engine` directly; the Engine singletons (`SimulationEngine`, `SimulationStatistics`) are shared across the entire WASM session.
- The simulation tick loop runs inside `SimulationService` using `PeriodicTimer`; it calls `SimulationEngine.Instance.Tick()` on each interval.
- Routing strategy and network-formation strategy are swappable at runtime: the UI writes to `SimulationEngine.Router` / `SimulationEngine.NetworkBuilder`.

---

## Documentation hierarchy

Read `Engine/Documentation.md` for engine architecture and structure.  
Read `WebApp/Documentation.md` for the WebApp structure and UI component responsibilities.  
Each subfolder contains its own `Documentation.md` for folder-level detail.
