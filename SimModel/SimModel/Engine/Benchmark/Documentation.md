# Engine/Benchmark

The `Benchmark` folder contains the headless benchmarking subsystem. It can run multiple routing strategies against the same scenario and produce structured, JSON-serialisable results for comparison.

---

## Files

| File                     | Responsibility                                                                                                                                                                                                                                                       |
| ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BenchmarkConfig.cs`     | Fully serialisable scenario definition: initial device list, scheduled events, engine settings (including packet defaults for TTL and travel time), and the list of router names to compare.                                                                       |
| `BenchmarkRunner.cs`     | Headless tick loop. For each router in `BenchmarkConfig.RouterNames` it creates an isolated engine/statistics context, applies the config (visibility, packet limits, packet defaults, router), fires scheduled events at their ticks, runs the simulation for `DurationTicks`, captures a `BenchmarkResult`, and emits optional progress snapshots. Supports optional topology/vector overrides and bounded router-level parallel execution (`maxDegreeOfParallelism`). Returns a `BenchmarkSession`. |
| `BenchmarkRunProgress.cs`| Progress payload used by `BenchmarkRunner` callback consumers. Includes current router name/index, completed routers, current tick, duration, per-router progress, and overall progress. |
| `BenchmarkResult.cs`     | Per-router output: final metric values and the full `TickSnapshot[]` history for charting.                                                                                                                                                                           |
| `BenchmarkSession.cs`    | Root JSON document combining `BenchmarkConfig` with all `BenchmarkResult` entries. Self-contained for saving and sharing.                                                                                                                                            |
| `BenchmarkEventEntry.cs` | Tick-stamped event union. Supported types: `ToggleBenchmarkEvent` (Hub sends a `ControlPacket`), `RemoveDeviceBenchmarkEvent` (simulates node failure), `AddDeviceBenchmarkEvent` (simulates node join). Uses `System.Text.Json` polymorphic `$type` discriminators. |
| `DeviceBenchmarkDto.cs`  | Serialisable record describing a device (type, name, position, frequency). Used inside `BenchmarkConfig` and `AddDeviceBenchmarkEvent`. Supports `with`-expression copying.                                                                                          |

---

## Benchmark flow

```
BenchmarkConfig
  +- BenchmarkRunner.Run()
       for each router:
      1. Create isolated engine/statistics context
      2. Engine.Reset() + Statistics.Reset()
      3. Apply network builder/vector overrides (or current engine defaults)
      4. Apply settings + register initial devices
      5. Tick loop [0 .. DurationTicks]:
        - fire scheduled BenchmarkEventEntry at matching ticks
        - Engine.Tick()
        - optionally publish `BenchmarkRunProgress`
      6. Capture BenchmarkResult (metrics + TickSnapshot[])
  +- Optional: run multiple routers in parallel with bounded degree
  +- Returns BenchmarkSession { Config, Results[] }
```

---

## Extension points

Add new `BenchmarkEventEntry` subtypes to schedule additional in-simulation actions (e.g. topology rebuild, router swap mid-run).

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
