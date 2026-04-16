# ConsoleUI

The `ConsoleUI` folder contains a standalone .NET console application for headless benchmark execution.

It is the console counterpart of the WebApp benchmark flow and reuses the same `Engine.Benchmark` subsystem.

---

## Purpose and boundary

- Run realistic multi-router benchmark scenarios from the terminal.
- Keep scenario assumptions close to protocol documentation and baseline vector research artifacts.
- Provide an interactive menu flow for selecting presets, output directory, and run actions without manual CLI flags.
- Provide an integrated case generator that creates deterministic benchmark configurations from prompt inputs.
- Use multi-core execution in CLI mode for faster scenario sweeps via bounded parallel workers.
- Produce rich terminal output for progress and result comparison.
- Emit machine-readable session JSON and human-readable HTML report artifacts.

This project does not host simulation UI widgets and does not depend on Blazor components.

---

## Files

| File | Responsibility |
| --- | --- |
| `ConsoleUI.csproj` | Console executable project file. Targets `net9.0`, references `Engine`, and adds `Spectre.Console` for structured terminal rendering. |
| `Program.cs` | Application entry point. Supports both CLI mode and interactive menu mode, runs selected scenarios, writes one session JSON per scenario, and writes one combined HTML report for the whole command. CLI mode supports bounded multi-core scheduling via `--parallelism` for concurrent scenario workers; when only one scenario is selected it can run router benchmarks in parallel. Includes interactive prompts for built-in selection, generated-case creation, output-directory management, topology sweep, and multi-seed sweep configuration. |
| `BenchmarkScenarioFactory.cs` | Builds realistic benchmark scenarios (device layouts, event timelines, packet defaults, router list, topology builder, swarm vector). Also exposes generated-case definitions used by interactive case generation. Built-in scenarios use the same visibility graph builder (`FullMeshNetworkBuilder`) for unbiased cross-router comparison. |
| `HtmlCombinedReportWriter.cs` | Generates one responsive self-contained combined HTML report for all scenario/seed/topology runs in the command, with run selector, summary table, detailed metrics, charts, event preview, and SVG topology snapshots. |
| `HtmlReportWriter.cs` | Legacy single-scenario report generator retained for compatibility; normal ConsoleUI flow now uses combined report generation. |

---

## Runtime flow

CLI mode (`dotnet run --project ConsoleUI/ConsoleUI.csproj -- [options]`):

1. Parse command-line options (`--scenario`, `--output`, `--interactive`, `--list`, `--help`, `--topologies`, `--seed-count`, `--seed-start`, `--seed-step`, `--parallelism`).
2. Build scenario set from `BenchmarkScenarioFactory`.
3. If topology sweep options are enabled, expand each selected scenario across topology profiles (`fullmesh`, `mst`, `k3`).
4. If seed sweep options are enabled, expand each selected scenario into deterministic seed variants.
5. Execute selected scenarios with bounded workers:
   - when multiple scenarios are selected, run scenarios concurrently up to `--parallelism`;
   - when a single scenario is selected, run router benchmarks with `maxDegreeOfParallelism = --parallelism`;
   - persist one session JSON per scenario.
6. Print per-scenario and final summary tables.
7. Generate one combined HTML report that aggregates all selected scenario runs.

Interactive mode (`dotnet run --project ConsoleUI/ConsoleUI.csproj` or `--interactive`):

1. Show an action menu: run built-in scenarios, generate case and run, list scenarios, change output directory, exit.
2. For generated-case runs, prompt for deterministic case parameters:
   - node count, visibility, duration, packet defaults;
   - event density, seed, topology profile, vector profile;
   - router set.
3. Prompt for optional topology sweep profiles before run.
4. Prompt for optional multi-seed sweep settings (count/start/step) before run.
5. Optionally export generated-case JSON to `benchmark-artifacts/generated-cases/` before execution.
6. Execute selected scenarios via the same benchmark runtime path as CLI mode.

---

## Parent

See `../Documentation.md` for solution-level structure and project roles.
