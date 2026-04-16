# WebApp/WebApp.Client

`WebApp.Client` is the Blazor WebAssembly project that serves as the interactive UI for the simulation. It runs entirely in the browser via .NET WASM and references `Engine` directly  -  there is no server component for the simulation.

---

## Subfolders

| Subfolder   | Responsibility                                                                                                               |
| ----------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `Layout/`   | Blazor layout components. Contains `MainLayout.razor`, which defines the application shell with the top bar and body region. |
| `Pages/`    | Routable Blazor pages. Contains `Home.razor`, the single application page.                                                   |
| `Services/` | Client-side services: simulation driver, config model, benchmark service, presets, and form models. Includes router selection defaults (`SwarmProtocol`, `SmartFlooding`, `Flooding`). |
| `Shared/`   | Reusable Blazor components: network graph, device table, packet table, statistics panel, and all modal dialogs.              |
| `wwwroot/`  | Static assets served to the browser: `app.css`, `index.html`, `404.html`.                                                    |

---

## Entry point

`Program.cs` registers `SimulationService` and `BenchmarkService` as scoped services on the Blazor WASM DI container, then launches the app with `App.razor` as the root component.

---

## Key files

| File             | Responsibility                                                           |
| ---------------- | ------------------------------------------------------------------------ |
| `Program.cs`     | DI setup and WASM host startup.                                          |
| `App.razor`      | Root Blazor component; sets up routing via `Router`.                     |
| `_Imports.razor` | Global `@using` directives shared across all components in this project. |

---

## Parent

See `WebApp/Documentation.md` for full WebApp architecture and component responsibilities.
