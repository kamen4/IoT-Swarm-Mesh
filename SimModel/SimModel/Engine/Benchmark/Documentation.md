# Engine/Benchmark

The `Benchmark` folder contains the headless benchmarking subsystem. It can run multiple routing strategies against the same scenario and produce structured, JSON-serialisable results for comparison.

---

## Files

| File                     | Responsibility                                                                                                                                                                                                                                                       |
| ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BenchmarkConfig.cs`     | Fully serialisable scenario definition: initial device list, scheduled events, engine settings, and the list of router names to compare.                                                                                                                             |
| `BenchmarkRunner.cs`     | Headless tick loop. For each router in `BenchmarkConfig.RouterNames` it resets the engine, applies the config, fires scheduled events at their ticks, runs the simulation for `DurationTicks`, and captures a `BenchmarkResult`. Returns a `BenchmarkSession`.       |
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
         1. Engine.Reset() + Statistics.Reset()
         2. Apply settings + register initial devices
         3. Tick loop [0 .. DurationTicks]:
              - fire scheduled BenchmarkEventEntry at matching ticks
              - Engine.Tick()
         4. Capture BenchmarkResult (metrics + TickSnapshot[])
  +- Returns BenchmarkSession { Config, Results[] }
```

---

## Extension points

Add new `BenchmarkEventEntry` subtypes to schedule additional in-simulation actions (e.g. topology rebuild, router swap mid-run).

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
