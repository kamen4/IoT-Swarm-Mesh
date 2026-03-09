# WebApp/WebApp.Client/Pages

Contains all routable Blazor pages. Currently the application is a single-page app, so this folder has exactly one page.

---

## Files

| File         | Responsibility                                                                                                                                                                                                                                                                                                                                                                                                                         |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Home.razor` | The single application page, routed at `/`. Hosts the top bar (tick counter, start/stop, reset, preset selector, settings, benchmark, docs), the two-column main grid (left: `DeviceTable` + `PacketTable`; right: `NetworkGraph`), the collapsible stats panel (`StatisticsPanel`), and all modal dialogs. Subscribes to `SimulationService.StateChanged` and calls `StateHasChanged()` to trigger reactive re-renders on every tick. |

---

## Parent

See `WebApp/WebApp.Client/Documentation.md` for the overall client project structure.  
See `WebApp/Documentation.md` for a detailed breakdown of the `Home.razor` top bar, grid layout, and modal wiring.
