using Engine.Benchmark;
using Engine.Routers;
using Spectre.Console;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleUI;

/// <summary>
/// Console entry point for protocol benchmark execution and report generation.
/// </summary>
internal static class Program
{
    private static readonly IReadOnlyDictionary<string, IPacketRouter> AvailableRouters =
        new IPacketRouter[]
        {
            new SwarmProtocolPacketRouter(),
            new SmartFloodingPacketRouter(),
            new FloodingPacketRouter(),
        }
        .ToDictionary(r => r.Name, StringComparer.Ordinal);

    private static readonly JsonSerializerOptions SessionJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Runs selected benchmark scenarios, writes per-scenario session JSON,
    /// and writes one combined HTML report for the whole command.
    /// </summary>
    /// <param name="args">CLI arguments.</param>
    /// <returns>Process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var options = CommandLineOptions.Parse(args);
        var scenarios = BenchmarkScenarioFactory.CreateScenarios();

        if (options.ShowHelp)
        {
            RenderHelp();
            return 0;
        }

        if (options.ListOnly)
        {
            RenderScenarioList(scenarios);
            return 0;
        }

        if (options.Interactive || args.Length == 0)
            return await RunInteractiveAsync(scenarios, options.OutputDirectory);

        var selectedScenarios = SelectScenarios(scenarios, options.ScenarioSelector);
        if (selectedScenarios.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No scenarios matched the selector.[/]");
            return 2;
        }

        var scenariosToRun = selectedScenarios;

        if (options.TopologyProfiles.Count > 0)
            scenariosToRun = BuildTopologySweepScenarios(scenariosToRun, options.TopologyProfiles);

        if (options.SeedCount.HasValue || options.SeedStart.HasValue || options.SeedStep.HasValue)
        {
            var seedCount = Math.Max(1, options.SeedCount ?? 1);
            var seedStart = Math.Max(1, options.SeedStart ?? 1);
            var seedStep = Math.Max(1, options.SeedStep ?? 1);

            scenariosToRun = BuildSeedSweepScenarios(
                scenariosToRun,
                new SeedSweepOptions(seedStart, seedCount, seedStep));
        }

        var outputDirectory = Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        var maxParallelism = Math.Max(1, options.Parallelism ?? Environment.ProcessorCount);

        RenderBanner(outputDirectory, scenariosToRun.Count);

        IReadOnlyList<RunArtifact> artifacts;
        string combinedReportPath;
        try
        {
            artifacts = await RunScenariosAsync(scenariosToRun, outputDirectory, maxParallelism);
            combinedReportPath = await HtmlCombinedReportWriter.WriteAsync(
                outputDirectory,
                artifacts.Select(static a =>
                    (a.Scenario, a.Session, a.Duration, a.JsonPath)).ToArray());
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Benchmark run failed.[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
            return 1;
        }

        RenderFinalSummary(artifacts, outputDirectory, combinedReportPath);
        return 0;
    }

    private enum InteractiveAction
    {
        RunBuiltIn,
        GenerateAndRun,
        ListBuiltIn,
        ChangeOutput,
        Exit,
    }

    private enum GeneratedCaseExecutionMode
    {
        RunOnly,
        SaveAndRun,
        SaveOnly,
    }

    private sealed record SeedSweepOptions(int SeedStart, int SeedCount, int SeedStep);

    private sealed record GeneratedTopologyVariant(int TotalNodes, int VisibilityDistance);

    /// <summary>
    /// Runs the interactive menu loop for ConsoleUI.
    /// </summary>
    /// <param name="scenarios">Built-in scenario catalog.</param>
    /// <param name="initialOutputDirectory">Initial output folder path.</param>
    /// <returns>Process exit code.</returns>
    private static async Task<int> RunInteractiveAsync(
        IReadOnlyList<BenchmarkScenario> scenarios,
        string initialOutputDirectory)
    {
        var outputDirectory = Path.GetFullPath(initialOutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        while (true)
        {
            RenderBanner(outputDirectory, scenarios.Count);

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<InteractiveAction>()
                    .Title("[bold]Choose action[/]")
                    .UseConverter(action => action switch
                    {
                        InteractiveAction.RunBuiltIn => "Run built-in scenarios",
                        InteractiveAction.GenerateAndRun => "Generate case and run",
                        InteractiveAction.ListBuiltIn => "List built-in scenarios",
                        InteractiveAction.ChangeOutput => "Change output directory",
                        _ => "Exit",
                    })
                    .AddChoices(
                        InteractiveAction.RunBuiltIn,
                        InteractiveAction.GenerateAndRun,
                        InteractiveAction.ListBuiltIn,
                        InteractiveAction.ChangeOutput,
                        InteractiveAction.Exit));

            if (action == InteractiveAction.Exit)
                return 0;

            if (action == InteractiveAction.ListBuiltIn)
            {
                RenderScenarioList(scenarios);
                WaitForEnter();
                continue;
            }

            if (action == InteractiveAction.ChangeOutput)
            {
                outputDirectory = PromptOutputDirectory(outputDirectory);
                continue;
            }

            try
            {
                IReadOnlyList<BenchmarkScenario> selected;
                var maxParallelism = 1;
                var shouldRun = true;

                if (action == InteractiveAction.RunBuiltIn)
                {
                    selected = PromptBuiltInScenarioSelection(scenarios);
                    if (selected.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No scenarios selected.[/]");
                        WaitForEnter();
                        continue;
                    }

                    var defaultProfiles = selected
                        .Select(BenchmarkScenarioFactory.DetectTopologyProfile)
                        .Distinct()
                        .ToList();

                    var topologyProfiles = PromptTopologySweepProfiles(
                        defaultProfiles,
                        promptTitle: "Topology sweep profiles for selected scenarios");

                    selected = BuildTopologySweepScenarios(selected, topologyProfiles);

                    var seedSweep = PromptSeedSweepOptions(
                        defaultSeedStart: 1,
                        promptTitle: "Seed sweep count for selected scenarios [grey](1 = disabled)[/]");

                    selected = BuildSeedSweepScenarios(selected, seedSweep);
                }
                else
                {
                    var definition = PromptGeneratedScenarioDefinition();
                    var topologyVariants = PromptGeneratedTopologyVariants(
                        defaultNodes: definition.TotalNodes,
                        defaultVisibilityDistance: definition.VisibilityDistance);

                    var generatedScenarios = BuildGeneratedTopologyScenarios(definition, topologyVariants);

                    var seedSweep = PromptSeedSweepOptions(
                        defaultSeedStart: definition.Seed <= 0 ? 1 : definition.Seed,
                        promptTitle: "Seed sweep count for generated case [grey](1 = disabled)[/]");

                    selected = seedSweep.SeedCount <= 1
                        ? generatedScenarios
                        : BuildSeedSweepScenarios(generatedScenarios, seedSweep);

                    var executionMode = PromptGeneratedCaseExecutionMode();

                    if (executionMode is GeneratedCaseExecutionMode.SaveAndRun or GeneratedCaseExecutionMode.SaveOnly)
                    {
                        var casePath = await WriteGeneratedCaseJsonAsync(
                            outputDirectory,
                            definition,
                            generatedScenarios[0],
                            topologyVariants,
                            seedSweep,
                            selected);
                        AnsiConsole.MarkupLine($"[green]Saved generated case:[/] {Markup.Escape(casePath)}");
                    }

                    if (executionMode == GeneratedCaseExecutionMode.SaveOnly)
                    {
                        shouldRun = false;
                        AnsiConsole.MarkupLine("[grey]Generated case saved without benchmark execution.[/]");
                    }
                    else
                    {
                        maxParallelism = PromptInteractiveParallelism(
                            defaultValue: 1,
                            promptTitle: "Parallelism for generated run [grey](1 = sequential)[/]");
                    }
                }

                if (shouldRun)
                {
                    var artifacts = await RunScenariosAsync(selected, outputDirectory, maxParallelism);
                    var combinedReportPath = await HtmlCombinedReportWriter.WriteAsync(
                        outputDirectory,
                        artifacts.Select(static a =>
                            (a.Scenario, a.Session, a.Duration, a.JsonPath)).ToArray());

                    RenderFinalSummary(artifacts, outputDirectory, combinedReportPath);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Operation failed.[/]");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
            }

            WaitForEnter();
        }
    }

    /// <summary>
    /// Runs all provided scenarios and returns produced artifacts.
    /// </summary>
    /// <param name="scenarios">Scenarios to execute.</param>
    /// <param name="outputDirectory">Artifact output directory.</param>
    /// <param name="maxParallelism">Maximum number of concurrent workers.</param>
    /// <returns>One artifact record per scenario.</returns>
    private static async Task<IReadOnlyList<RunArtifact>> RunScenariosAsync(
        IReadOnlyList<BenchmarkScenario> scenarios,
        string outputDirectory,
        int maxParallelism)
    {
        if (scenarios.Count == 0)
            return [];

        var safeParallelism = Math.Clamp(maxParallelism, 1, Math.Max(1, Environment.ProcessorCount));

        if (safeParallelism <= 1)
        {
            var sequentialArtifacts = new List<RunArtifact>(scenarios.Count);
            foreach (var scenario in scenarios)
            {
                var artifact = await RunScenarioAsync(
                    scenario,
                    outputDirectory,
                    renderOutput: true,
                    routerParallelism: 1);

                sequentialArtifacts.Add(artifact);
            }

            return sequentialArtifacts;
        }

        if (scenarios.Count == 1)
        {
            var artifact = await RunScenarioAsync(
                scenarios[0],
                outputDirectory,
                renderOutput: true,
                routerParallelism: safeParallelism);

            return [artifact];
        }

        AnsiConsole.MarkupLine($"[grey]Parallel scenario workers:[/] {safeParallelism}");

        var artifacts = new RunArtifact[scenarios.Count];
        using var gate = new SemaphoreSlim(Math.Min(safeParallelism, scenarios.Count));
        var progressSync = new object();

        await AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(),
            ])
            .StartAsync(async context =>
            {
                var scenarioTasks = scenarios
                    .Select(s => context.AddTask(
                        $"[blue]{Markup.Escape(TruncateProgressLabel(s.Name, 40))}[/]",
                        maxValue: Math.Max(1, s.Config.RouterNames.Count)))
                    .ToArray();

                var tasks = new List<Task>(scenarios.Count);

                for (var i = 0; i < scenarios.Count; i++)
                {
                    await gate.WaitAsync();
                    var index = i;
                    var scenario = scenarios[index];
                    var progressTask = scenarioTasks[index];

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            artifacts[index] = await RunScenarioAsync(
                                scenario,
                                outputDirectory,
                                renderOutput: false,
                                routerParallelism: 1,
                                onProgress: progress =>
                                {
                                    var routerName = string.IsNullOrWhiteSpace(progress.CurrentRouterName)
                                        ? "starting"
                                        : progress.CurrentRouterName;

                                    lock (progressSync)
                                    {
                                        progressTask.MaxValue = Math.Max(1, progress.TotalRouters);
                                        progressTask.Value = Math.Clamp(
                                            progress.CompletedRouters + progress.RouterProgress,
                                            0.0,
                                            progressTask.MaxValue);

                                        progressTask.Description =
                                            $"[blue]{Markup.Escape(TruncateProgressLabel(scenario.Name, 30))}[/] " +
                                            $"[grey]{Markup.Escape(TruncateProgressLabel(routerName, 22))} " +
                                            $"{progress.CurrentTick}/{Math.Max(1, progress.DurationTicks)}[/]";
                                    }
                                });

                            lock (progressSync)
                            {
                                progressTask.Value = progressTask.MaxValue;
                                progressTask.Description =
                                    $"[green]{Markup.Escape(TruncateProgressLabel(scenario.Name, 40))}[/] [grey]done[/]";
                            }
                        }
                        finally
                        {
                            gate.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            });

        foreach (var artifact in artifacts)
        {
            RenderScenarioArtifact(artifact);
        }

        return artifacts;
    }

    /// <summary>
    /// Builds seed variants for each selected scenario.
    /// </summary>
    /// <param name="scenarios">Source scenarios.</param>
    /// <param name="options">Seed sweep options.</param>
    /// <returns>Expanded scenario list.</returns>
    private static IReadOnlyList<BenchmarkScenario> BuildSeedSweepScenarios(
        IReadOnlyList<BenchmarkScenario> scenarios,
        SeedSweepOptions options)
    {
        if (options.SeedCount <= 1)
            return scenarios;

        var expanded = new List<BenchmarkScenario>(scenarios.Count * options.SeedCount);

        foreach (var scenario in scenarios)
        {
            for (var i = 0; i < options.SeedCount; i++)
            {
                var seed = options.SeedStart + i * options.SeedStep;
                var variant = BenchmarkScenarioFactory.CreateSeedVariant(scenario, seed);
                expanded.Add(variant);
            }
        }

        return expanded;
    }

    /// <summary>
    /// Builds topology variants for each selected scenario.
    /// </summary>
    /// <param name="scenarios">Source scenarios.</param>
    /// <param name="profiles">Topology profiles to expand to.</param>
    /// <returns>Expanded scenario list.</returns>
    private static IReadOnlyList<BenchmarkScenario> BuildTopologySweepScenarios(
        IReadOnlyList<BenchmarkScenario> scenarios,
        IReadOnlyList<ScenarioTopologyProfile> profiles)
    {
        var normalizedProfiles = profiles
            .Distinct()
            .ToList();

        if (normalizedProfiles.Count == 0)
            return scenarios;

        var expanded = new List<BenchmarkScenario>(scenarios.Count * normalizedProfiles.Count);

        foreach (var scenario in scenarios)
        {
            var currentProfile = BenchmarkScenarioFactory.DetectTopologyProfile(scenario);

            if (normalizedProfiles.Count == 1 && normalizedProfiles[0] == currentProfile)
            {
                expanded.Add(scenario);
                continue;
            }

            var baseDefinition = BenchmarkScenarioFactory.CreateDefinitionFromScenario(scenario);

            foreach (var profile in normalizedProfiles)
            {
                var definition = baseDefinition.Clone();
                definition.TopologyProfile = profile;

                var profileText = DescribeTopologyProfile(profile);
                var profileToken = TopologyProfileToken(profile);

                var variant = BenchmarkScenarioFactory.CreateGeneratedScenario(
                    definition,
                    slugOverride: $"{scenario.Slug}-topology-{profileToken}",
                    nameOverride: $"{scenario.Name} [topology: {profileText}]",
                    descriptionOverride: $"{scenario.Description} Topology variant: {profileText}.");

                expanded.Add(variant);
            }
        }

        return expanded;
    }

    private static IReadOnlyList<ScenarioTopologyProfile> PromptTopologySweepProfiles(
        IReadOnlyCollection<ScenarioTopologyProfile> defaultProfiles,
        string promptTitle)
    {
        var prompt = new MultiSelectionPrompt<ScenarioTopologyProfile>()
            .Title($"[bold]{promptTitle}[/]")
            .InstructionsText("[grey](Space to toggle, Enter to accept)[/]")
            .NotRequired()
            .UseConverter(static p => DescribeTopologyProfile(p))
            .AddChoices(Enum.GetValues<ScenarioTopologyProfile>());

        foreach (var profile in defaultProfiles.Distinct())
            prompt.Select(profile);

        var selected = AnsiConsole.Prompt(prompt)
            .Distinct()
            .ToList();

        if (selected.Count == 0)
            selected.Add(defaultProfiles.FirstOrDefault(ScenarioTopologyProfile.FullMesh));

        return selected;
    }

    private static SeedSweepOptions PromptSeedSweepOptions(int defaultSeedStart, string promptTitle)
    {
        var seedCount = AnsiConsole.Prompt(
            new TextPrompt<int>(promptTitle)
                .DefaultValue(1)
                .Validate(v => v is < 1 or > 128
                    ? ValidationResult.Error("Enter value in [1..128].")
                    : ValidationResult.Success()));

        if (seedCount <= 1)
            return new SeedSweepOptions(Math.Max(1, defaultSeedStart), 1, 1);

        var seedStart = AnsiConsole.Prompt(
            new TextPrompt<int>("Seed start")
                .DefaultValue(Math.Max(1, defaultSeedStart))
                .Validate(v => v is < 1 or > 1_000_000
                    ? ValidationResult.Error("Enter value in [1..1000000].")
                    : ValidationResult.Success()));

        var seedStep = AnsiConsole.Prompt(
            new TextPrompt<int>("Seed step")
                .DefaultValue(1)
                .Validate(v => v is < 1 or > 100_000
                    ? ValidationResult.Error("Enter value in [1..100000].")
                    : ValidationResult.Success()));

        return new SeedSweepOptions(seedStart, seedCount, seedStep);
    }

    private static IReadOnlyList<GeneratedTopologyVariant> PromptGeneratedTopologyVariants(
        int defaultNodes,
        int defaultVisibilityDistance)
    {
        var topologyCount = AnsiConsole.Prompt(
            new TextPrompt<int>("Topology cases count [grey](nodes + visibility per case)[/]")
                .DefaultValue(1)
                .Validate(v => v is < 1 or > 24
                    ? ValidationResult.Error("Enter value in [1..24].")
                    : ValidationResult.Success()));

        var variants = new List<GeneratedTopologyVariant>(topologyCount);

        for (var i = 0; i < topologyCount; i++)
        {
            var caseTitle = topologyCount == 1
                ? "Topology case"
                : $"Topology case #{i + 1}";

            var nodes = AnsiConsole.Prompt(
                new TextPrompt<int>($"{caseTitle}: total nodes [grey](including Hub)[/]")
                    .DefaultValue(defaultNodes)
                    .Validate(v => v is < 8 or > 240
                        ? ValidationResult.Error("Enter value in [8..240].")
                        : ValidationResult.Success()));

            var visibility = AnsiConsole.Prompt(
                new TextPrompt<int>($"{caseTitle}: visibility distance")
                    .DefaultValue(defaultVisibilityDistance)
                    .Validate(v => v is < 80 or > 900
                        ? ValidationResult.Error("Enter value in [80..900].")
                        : ValidationResult.Success()));

            variants.Add(new GeneratedTopologyVariant(nodes, visibility));

            defaultNodes = nodes;
            defaultVisibilityDistance = visibility;
        }

        return variants;
    }

    private static IReadOnlyList<BenchmarkScenario> BuildGeneratedTopologyScenarios(
        GeneratedScenarioDefinition baseDefinition,
        IReadOnlyList<GeneratedTopologyVariant> topologyVariants)
    {
        var variants = topologyVariants.Count == 0
            ? [new GeneratedTopologyVariant(baseDefinition.TotalNodes, baseDefinition.VisibilityDistance)]
            : topologyVariants;

        var scenarios = new List<BenchmarkScenario>(variants.Count);

        for (var i = 0; i < variants.Count; i++)
        {
            var variant = variants[i];
            var definition = baseDefinition.Clone();
            definition.TotalNodes = variant.TotalNodes;
            definition.VisibilityDistance = variant.VisibilityDistance;
            definition.TopologyProfile = ScenarioTopologyProfile.KNearest10;

            var hasMultiple = variants.Count > 1;
            var titleSuffix = $"{variant.TotalNodes} nodes / vis {variant.VisibilityDistance}";

            var scenario = BenchmarkScenarioFactory.CreateGeneratedScenario(
                definition,
                slugOverride: hasMultiple
                    ? $"{definition.Name}-topology-{i + 1}-n{variant.TotalNodes}-v{variant.VisibilityDistance}"
                    : null,
                nameOverride: hasMultiple
                    ? $"{definition.Name} [topology {i + 1}: {titleSuffix}]"
                    : definition.Name,
                descriptionOverride: $"{definition.Description} Topology case: {titleSuffix}. Builder: k-nearest (k=10).")
                with
                {
                    TopologyBuilder = new KNearestNetworkBuilder(10),
                };

            scenarios.Add(scenario);
        }

        return scenarios;
    }

    private static GeneratedCaseExecutionMode PromptGeneratedCaseExecutionMode()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<GeneratedCaseExecutionMode>()
                .Title("[bold]Generated case action[/]")
                .UseConverter(mode => mode switch
                {
                    GeneratedCaseExecutionMode.RunOnly => "Run benchmark only",
                    GeneratedCaseExecutionMode.SaveAndRun => "Save case JSON and run benchmark",
                    _ => "Save case JSON only",
                })
                .AddChoices(
                    GeneratedCaseExecutionMode.RunOnly,
                    GeneratedCaseExecutionMode.SaveAndRun,
                    GeneratedCaseExecutionMode.SaveOnly));
    }

    private static int PromptInteractiveParallelism(int defaultValue, string promptTitle)
    {
        var cpuCount = Math.Max(1, Environment.ProcessorCount);
        var safeDefault = Math.Clamp(defaultValue, 1, cpuCount);

        return AnsiConsole.Prompt(
            new TextPrompt<int>(promptTitle)
                .DefaultValue(safeDefault)
                .Validate(v => v is < 1 || v > cpuCount
                    ? ValidationResult.Error($"Enter value in [1..{cpuCount}].")
                    : ValidationResult.Success()));
    }

    /// <summary>
    /// Prompts for built-in scenario selection.
    /// </summary>
    /// <param name="scenarios">Built-in scenario catalog.</param>
    /// <returns>Selected scenarios in original order.</returns>
    private static IReadOnlyList<BenchmarkScenario> PromptBuiltInScenarioSelection(
        IReadOnlyList<BenchmarkScenario> scenarios)
    {
        var keyToScenario = scenarios.ToDictionary(
            s => $"{s.Name} [{s.Slug}]",
            s => s,
            StringComparer.Ordinal);

        var prompt = new MultiSelectionPrompt<string>()
            .Title("[bold]Select scenarios[/]")
            .InstructionsText("[grey](Space to toggle, Enter to run)[/]")
            .NotRequired()
            .AddChoices(keyToScenario.Keys);

        foreach (var key in keyToScenario.Keys)
            prompt.Select(key);

        var selectedKeys = AnsiConsole.Prompt(prompt);

        var selected = new List<BenchmarkScenario>();
        foreach (var scenario in scenarios)
        {
            var key = $"{scenario.Name} [{scenario.Slug}]";
            if (selectedKeys.Contains(key, StringComparer.Ordinal))
                selected.Add(scenario);
        }

        return selected;
    }

    /// <summary>
    /// Prompts interactive inputs for a generated benchmark case.
    /// </summary>
    /// <returns>Generated scenario definition.</returns>
    private static GeneratedScenarioDefinition PromptGeneratedScenarioDefinition()
    {
        var defaults = BenchmarkScenarioFactory.CreateDefaultGeneratedDefinition();

        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("Case name")
                .DefaultValue(defaults.Name));

        var description = AnsiConsole.Prompt(
            new TextPrompt<string>("Case description")
                .DefaultValue(defaults.Description));

        var durationTicks = AnsiConsole.Prompt(
            new TextPrompt<long>("Duration ticks")
                .DefaultValue(defaults.DurationTicks)
                .Validate(v => v is < 120 or > 20_000
                    ? ValidationResult.Error("Enter value in [120..20000].")
                    : ValidationResult.Success()));

        var defaultTtl = AnsiConsole.Prompt(
            new TextPrompt<int>("Default packet TTL")
                .DefaultValue(defaults.DefaultTtl)
                .Validate(v => v is < 4 or > 64
                    ? ValidationResult.Error("Enter value in [4..64].")
                    : ValidationResult.Success()));

        var ticksToTravel = AnsiConsole.Prompt(
            new TextPrompt<long>("Ticks to travel per hop")
                .DefaultValue(defaults.TicksToTravel)
                .Validate(v => v is < 1 or > 40
                    ? ValidationResult.Error("Enter value in [1..40].")
                    : ValidationResult.Success()));

        var emitterSharePercent = AnsiConsole.Prompt(
            new TextPrompt<int>("Emitter share percent")
                .DefaultValue((int)Math.Round(defaults.EmitterShare * 100.0, MidpointRounding.AwayFromZero))
                .Validate(v => v is < 0 or > 100
                    ? ValidationResult.Error("Enter value in [0..100].")
                    : ValidationResult.Success()));

        var generatorMinTicks = AnsiConsole.Prompt(
            new TextPrompt<int>("Generator min ticks")
                .DefaultValue(defaults.GeneratorMinTicks)
                .Validate(v => v is < 1 or > 10_000
                    ? ValidationResult.Error("Enter value in [1..10000].")
                    : ValidationResult.Success()));

        var generatorMaxTicks = AnsiConsole.Prompt(
            new TextPrompt<int>("Generator max ticks")
                .DefaultValue(defaults.GeneratorMaxTicks)
                .Validate(v => v is < 1 or > 10_000
                    ? ValidationResult.Error("Enter value in [1..10000].")
                    : ValidationResult.Success()));

        var emitterMinTicks = AnsiConsole.Prompt(
            new TextPrompt<int>("Emitter min ticks")
                .DefaultValue(defaults.EmitterMinTicks)
                .Validate(v => v is < 1 or > 10_000
                    ? ValidationResult.Error("Enter value in [1..10000].")
                    : ValidationResult.Success()));

        var emitterMaxTicks = AnsiConsole.Prompt(
            new TextPrompt<int>("Emitter max ticks")
                .DefaultValue(defaults.EmitterMaxTicks)
                .Validate(v => v is < 1 or > 10_000
                    ? ValidationResult.Error("Enter value in [1..10000].")
                    : ValidationResult.Success()));

        var minEvents = AnsiConsole.Prompt(
            new TextPrompt<int>("Minimum events")
                .DefaultValue(defaults.MinEvents)
                .Validate(v => v is < 1 or > 500
                    ? ValidationResult.Error("Enter value in [1..500].")
                    : ValidationResult.Success()));

        var maxEvents = AnsiConsole.Prompt(
            new TextPrompt<int>("Maximum events")
                .DefaultValue(defaults.MaxEvents)
                .Validate(v => v is < 1 or > 500
                    ? ValidationResult.Error("Enter value in [1..500].")
                    : ValidationResult.Success()));

        var seed = AnsiConsole.Prompt(
            new TextPrompt<int>("Deterministic seed [grey](0 = auto)[/]")
                .DefaultValue(defaults.Seed));

        var vectorProfile = AnsiConsole.Prompt(
            new SelectionPrompt<ScenarioVectorProfile>()
                .Title("Swarm vector profile")
                .UseConverter(static p => p switch
                {
                    ScenarioVectorProfile.BaselineFixed => "Baseline fixed vector",
                    _ => "Convergence tuned vector",
                })
                .AddChoices(Enum.GetValues<ScenarioVectorProfile>()));

        var routerPrompt = new MultiSelectionPrompt<string>()
            .Title("[bold]Routers to include[/]")
            .InstructionsText("[grey](Space to toggle, Enter to accept)[/]")
            .NotRequired()
            .AddChoices(AvailableRouters.Keys.OrderBy(static n => n, StringComparer.Ordinal));

        foreach (var routerName in defaults.RouterNames)
            routerPrompt.Select(routerName);

        var selectedRouters = AnsiConsole.Prompt(routerPrompt).ToList();
        if (selectedRouters.Count == 0)
            selectedRouters = defaults.RouterNames.ToList();

        return new GeneratedScenarioDefinition
        {
            Name = name,
            Description = description,
            TotalNodes = defaults.TotalNodes,
            VisibilityDistance = defaults.VisibilityDistance,
            DurationTicks = durationTicks,
            DefaultTtl = defaultTtl,
            TicksToTravel = ticksToTravel,
            EmitterShare = emitterSharePercent / 100.0,
            GeneratorMinTicks = Math.Min(generatorMinTicks, generatorMaxTicks),
            GeneratorMaxTicks = Math.Max(generatorMinTicks, generatorMaxTicks),
            EmitterMinTicks = Math.Min(emitterMinTicks, emitterMaxTicks),
            EmitterMaxTicks = Math.Max(emitterMinTicks, emitterMaxTicks),
            MinEvents = Math.Min(minEvents, maxEvents),
            MaxEvents = Math.Max(minEvents, maxEvents),
            Seed = seed,
            TopologyProfile = ScenarioTopologyProfile.KNearest10,
            VectorProfile = vectorProfile,
            RouterNames = selectedRouters,
        };
    }

    /// <summary>
    /// Writes one generated-case JSON file for reproducibility.
    /// </summary>
    /// <param name="outputDirectory">Artifact root directory.</param>
    /// <param name="definition">User-selected generation settings.</param>
    /// <param name="scenario">Generated benchmark scenario.</param>
    /// <param name="topologyVariants">
    /// Requested topology variants (nodes and visibility) for this generation batch.
    /// </param>
    /// <param name="seedSweep">Seed sweep settings used for this run.</param>
    /// <param name="runScenarios">Actual scenarios executed.</param>
    /// <returns>Absolute path to the written JSON file.</returns>
    private static async Task<string> WriteGeneratedCaseJsonAsync(
        string outputDirectory,
        GeneratedScenarioDefinition definition,
        BenchmarkScenario scenario,
        IReadOnlyList<GeneratedTopologyVariant> topologyVariants,
        SeedSweepOptions seedSweep,
        IReadOnlyList<BenchmarkScenario> runScenarios)
    {
        var directory = Path.Combine(outputDirectory, "generated-cases");
        Directory.CreateDirectory(directory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var filePath = Path.Combine(directory, $"{scenario.Slug}_{timestamp}_case.json");

        var payload = new
        {
            generatedAtUtc = DateTime.UtcNow,
            scenarioSlug = scenario.Slug,
            scenarioName = scenario.Name,
            topologyBuilder = scenario.TopologyBuilder.Name,
            vector = scenario.Vector,
            definition,
            benchmarkConfig = scenario.Config,
            seedSweep = new
            {
                seedSweep.SeedStart,
                seedSweep.SeedCount,
                seedSweep.SeedStep,
            },
            topologyCases = topologyVariants
                .Select(static (v, i) => new
                {
                    index = i + 1,
                    v.TotalNodes,
                    v.VisibilityDistance,
                    topologyBuilder = "K-Nearest (k=10)",
                })
                .ToArray(),
            runScenarioSlugs = runScenarios.Select(s => s.Slug).ToArray(),
        };

        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(payload, SessionJsonOptions));
        return filePath;
    }

    /// <summary>
    /// Prompts and validates a new output directory.
    /// </summary>
    /// <param name="currentOutputDirectory">Current output directory.</param>
    /// <returns>New absolute output directory path.</returns>
    private static string PromptOutputDirectory(string currentOutputDirectory)
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("Output directory")
                .DefaultValue(currentOutputDirectory));

        var outputDirectory = Path.GetFullPath(input);
        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
    }

    /// <summary>
    /// Waits for Enter key in interactive mode.
    /// </summary>
    private static void WaitForEnter()
    {
        AnsiConsole.MarkupLine("[grey]Press Enter to continue...[/]");
        Console.ReadLine();
    }

    /// <summary>
    /// Executes one scenario and emits its session JSON artifact.
    /// </summary>
    /// <param name="scenario">Scenario to execute.</param>
    /// <param name="outputDirectory">Artifact output directory.</param>
    /// <param name="renderOutput">
    /// When true, renders terminal output for this scenario.
    /// </param>
    /// <param name="routerParallelism">
    /// Maximum number of router workers for this scenario.
    /// </param>
    /// <param name="onProgress">
    /// Optional progress callback used by parallel orchestration paths.
    /// </param>
    private static async Task<RunArtifact> RunScenarioAsync(
        BenchmarkScenario scenario,
        string outputDirectory,
        bool renderOutput,
        int routerParallelism,
        Action<BenchmarkRunProgress>? onProgress = null)
    {
        var safeRouterParallelism = Math.Max(1, routerParallelism);

        if (renderOutput && safeRouterParallelism == 1)
        {
            var scenarioPanel = new Panel($"[bold]{Markup.Escape(scenario.Name)}[/]{Environment.NewLine}{Markup.Escape(scenario.Description)}")
            {
                Header = new PanelHeader("Scenario", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Teal),
            };

            AnsiConsole.Write(scenarioPanel);
        }

        var stopwatch = Stopwatch.StartNew();
        BenchmarkSession? session = null;

        if (renderOutput && safeRouterParallelism == 1)
        {
            await AnsiConsole.Progress()
                .AutoClear(true)
                .HideCompleted(false)
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn(),
                ])
                .StartAsync(async context =>
                {
                    var routerTask = context.AddTask("[green]Routers[/]", maxValue: Math.Max(1, scenario.Config.RouterNames.Count));
                    var tickTask = context.AddTask("[blue]Ticks[/]", maxValue: Math.Max(1, scenario.Config.DurationTicks));

                    session = await BenchmarkRunner.RunAsync(
                        scenario.Config,
                        AvailableRouters,
                        progress =>
                        {
                            var routerName = string.IsNullOrWhiteSpace(progress.CurrentRouterName)
                                ? "initializing"
                                : progress.CurrentRouterName;

                            routerTask.Description = $"[green]{Markup.Escape(routerName)}[/]";
                            routerTask.MaxValue = Math.Max(1, progress.TotalRouters);
                            routerTask.Value = Math.Clamp(
                                progress.CompletedRouters + progress.RouterProgress,
                                0.0,
                                routerTask.MaxValue);

                            tickTask.Description = $"[blue]Tick {progress.CurrentTick}/{progress.DurationTicks}[/]";
                            tickTask.MaxValue = Math.Max(1, progress.DurationTicks);
                            tickTask.Value = Math.Clamp((double)progress.CurrentTick, 0.0, tickTask.MaxValue);

                            onProgress?.Invoke(progress);
                        },
                        networkBuilder: scenario.TopologyBuilder,
                        swarmVector: scenario.Vector,
                        maxDegreeOfParallelism: 1);

                    routerTask.Value = routerTask.MaxValue;
                    tickTask.Value = tickTask.MaxValue;
                });
        }
        else
        {
            if (renderOutput && safeRouterParallelism > 1)
                AnsiConsole.MarkupLine($"[grey]Router workers for this scenario:[/] {safeRouterParallelism}");

            if (renderOutput && safeRouterParallelism > 1)
            {
                await AnsiConsole.Progress()
                    .AutoClear(true)
                    .HideCompleted(false)
                    .Columns(
                    [
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new RemainingTimeColumn(),
                        new SpinnerColumn(),
                    ])
                    .StartAsync(async context =>
                    {
                        var routerTask = context.AddTask(
                            "[green]Routers[/]",
                            maxValue: Math.Max(1, scenario.Config.RouterNames.Count));
                        var progressSync = new object();

                        session = await BenchmarkRunner.RunAsync(
                            scenario.Config,
                            AvailableRouters,
                            progress =>
                            {
                                var routerName = string.IsNullOrWhiteSpace(progress.CurrentRouterName)
                                    ? "starting"
                                    : progress.CurrentRouterName;

                                lock (progressSync)
                                {
                                    routerTask.Description = $"[green]{Markup.Escape(TruncateProgressLabel(routerName, 36))}[/]";
                                    routerTask.MaxValue = Math.Max(1, progress.TotalRouters);
                                    routerTask.Value = Math.Clamp(
                                        progress.CompletedRouters + progress.RouterProgress,
                                        0.0,
                                        routerTask.MaxValue);
                                }

                                onProgress?.Invoke(progress);
                            },
                            networkBuilder: scenario.TopologyBuilder,
                            swarmVector: scenario.Vector,
                            maxDegreeOfParallelism: safeRouterParallelism);

                        lock (progressSync)
                        {
                            routerTask.Value = routerTask.MaxValue;
                        }
                    });
            }
            else
            {
                session = await BenchmarkRunner.RunAsync(
                    scenario.Config,
                    AvailableRouters,
                    onProgress,
                    networkBuilder: scenario.TopologyBuilder,
                    swarmVector: scenario.Vector,
                    maxDegreeOfParallelism: safeRouterParallelism);
            }
        }

        stopwatch.Stop();

        if (session is null)
            throw new InvalidOperationException("Benchmark runner returned no session.");

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var sessionPath = Path.Combine(outputDirectory, $"{scenario.Slug}_{timestamp}_session.json");

        await File.WriteAllTextAsync(
            sessionPath,
            JsonSerializer.Serialize(session, SessionJsonOptions));

        var best = session.Results
            .OrderByDescending(r => r.DeliveryRate)
            .ThenBy(r => r.DuplicateDeliveries)
            .FirstOrDefault();

        var artifact = new RunArtifact(
            Scenario: scenario,
            Session: session,
            ScenarioName: scenario.Name,
            Duration: stopwatch.Elapsed,
            BestRouter: best?.RouterName ?? "n/a",
            BestDeliveryRate: best?.DeliveryRate ?? 0,
            JsonPath: sessionPath);

        if (renderOutput)
        {
            if (safeRouterParallelism == 1)
            {
                RenderScenarioResultTable(session, stopwatch.Elapsed);
                AnsiConsole.MarkupLine($"[green]JSON:[/] {Markup.Escape(sessionPath)}");
                AnsiConsole.WriteLine();
            }
            else
            {
                RenderScenarioArtifact(artifact);
            }
        }

        return artifact;
    }

    /// <summary>
    /// Renders a compact result table for a completed scenario.
    /// </summary>
    private static void RenderScenarioResultTable(BenchmarkSession session, TimeSpan elapsed)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Expand();

        table.AddColumn("Router");
        table.AddColumn(new TableColumn("Delivery %").RightAligned());
        table.AddColumn(new TableColumn("Delivered").RightAligned());
        table.AddColumn(new TableColumn("Expired").RightAligned());
        table.AddColumn(new TableColumn("Duplicates").RightAligned());
        table.AddColumn(new TableColumn("Avg hops").RightAligned());
        table.AddColumn(new TableColumn("Avg tick ms").RightAligned());

        var ordered = session.Results
            .OrderByDescending(r => r.DeliveryRate)
            .ThenBy(r => r.DuplicateDeliveries)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];
            var stylePrefix = i == 0 ? "[bold green]" : string.Empty;
            var styleSuffix = i == 0 ? "[/]" : string.Empty;

            table.AddRow(
                $"{stylePrefix}{Markup.Escape(item.RouterName)}{styleSuffix}",
                $"{stylePrefix}{item.DeliveryRate:0.##}{styleSuffix}",
                $"{stylePrefix}{item.TotalPacketsDelivered:0.##}{styleSuffix}",
                $"{stylePrefix}{item.TotalPacketsExpired:0.##}{styleSuffix}",
                $"{stylePrefix}{item.DuplicateDeliveries:0.##}{styleSuffix}",
                $"{stylePrefix}{item.AvgHopCount:0.##}{styleSuffix}",
                $"{stylePrefix}{item.AvgTickMs:0.###}{styleSuffix}");
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]Scenario elapsed:[/] {elapsed:mm\\:ss}");
    }

    /// <summary>
    /// Renders scenario heading, result table, and JSON artifact location.
    /// </summary>
    /// <param name="artifact">Completed run artifact.</param>
    private static void RenderScenarioArtifact(RunArtifact artifact)
    {
        var scenarioPanel = new Panel($"[bold]{Markup.Escape(artifact.Scenario.Name)}[/]{Environment.NewLine}{Markup.Escape(artifact.Scenario.Description)}")
        {
            Header = new PanelHeader("Scenario", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Teal),
        };

        AnsiConsole.Write(scenarioPanel);
        RenderScenarioResultTable(artifact.Session, artifact.Duration);
        AnsiConsole.MarkupLine($"[green]JSON:[/] {Markup.Escape(artifact.JsonPath)}");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Selects scenarios by slug or name from the selector expression.
    /// </summary>
    private static IReadOnlyList<BenchmarkScenario> SelectScenarios(
        IReadOnlyList<BenchmarkScenario> all,
        string selector)
    {
        if (string.Equals(selector, "all", StringComparison.OrdinalIgnoreCase))
            return all;

        var tokens = selector
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (tokens.Length == 0)
            return all;

        var selected = new List<BenchmarkScenario>();

        foreach (var token in tokens)
        {
            var match = all.FirstOrDefault(s =>
                string.Equals(s.Slug, token, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Name, token, StringComparison.OrdinalIgnoreCase));

            if (match is null)
                continue;

            if (selected.Any(s => string.Equals(s.Slug, match.Slug, StringComparison.OrdinalIgnoreCase)))
                continue;

            selected.Add(match);
        }

        return selected;
    }

    private static string DescribeTopologyProfile(ScenarioTopologyProfile profile)
    {
        return profile switch
        {
            ScenarioTopologyProfile.FullMesh => "Full mesh visibility graph",
            ScenarioTopologyProfile.VisibilityMst => "Visibility MST",
            ScenarioTopologyProfile.KNearest3 => "K-nearest (k=3)",
            _ => "K-nearest (k=10)",
        };
    }

    private static string TopologyProfileToken(ScenarioTopologyProfile profile)
    {
        return profile switch
        {
            ScenarioTopologyProfile.FullMesh => "fullmesh",
            ScenarioTopologyProfile.VisibilityMst => "mst",
            ScenarioTopologyProfile.KNearest3 => "k3",
            _ => "k10",
        };
    }

    private static string TruncateProgressLabel(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "n/a";

        var text = value.Trim();
        if (text.Length <= maxLength || maxLength < 4)
            return text;

        return string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }

    private static IReadOnlyList<ScenarioTopologyProfile> ParseTopologyProfiles(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        var tokens = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static x => x.Trim().ToLowerInvariant())
            .ToArray();

        if (tokens.Length == 0)
            return [];

        if (tokens.Contains("all", StringComparer.Ordinal))
            return Enum.GetValues<ScenarioTopologyProfile>();

        var selected = new List<ScenarioTopologyProfile>();

        foreach (var token in tokens)
        {
            var profile = token switch
            {
                "full" or "fullmesh" or "mesh" => ScenarioTopologyProfile.FullMesh,
                "mst" or "tree" or "visibility-mst" => ScenarioTopologyProfile.VisibilityMst,
                "k3" or "knn3" => ScenarioTopologyProfile.KNearest3,
                "k10" or "knn10" or "top10" or "k-nearest" or "knearest" => ScenarioTopologyProfile.KNearest10,
                _ => (ScenarioTopologyProfile?)null,
            };

            if (!profile.HasValue)
                continue;

            if (!selected.Contains(profile.Value))
                selected.Add(profile.Value);
        }

        return selected;
    }

    /// <summary>
    /// Prints startup banner and runtime context.
    /// </summary>
    private static void RenderBanner(string outputDirectory, int scenarioCount)
    {
        AnsiConsole.Write(
            new FigletText("Swarm Bench")
                .Centered()
                .Color(Color.Teal));

        var summary = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn(new GridColumn());

        summary.AddRow("Scenarios", scenarioCount.ToString(CultureInfo.InvariantCulture));
        summary.AddRow("Output", outputDirectory);

        var panel = new Panel(summary)
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader("Console Benchmark", Justify.Left),
            BorderStyle = new Style(Color.Grey),
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Prints final run summary and artifact pointers.
    /// </summary>
    private static void RenderFinalSummary(
        IReadOnlyList<RunArtifact> artifacts,
        string outputDirectory,
        string combinedReportPath)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Expand();

        table.AddColumn("Scenario");
        table.AddColumn(new TableColumn("Elapsed").RightAligned());
        table.AddColumn("Best router");
        table.AddColumn(new TableColumn("Best delivery %").RightAligned());

        foreach (var artifact in artifacts)
        {
            table.AddRow(
                Markup.Escape(artifact.ScenarioName),
                artifact.Duration.ToString("mm\\:ss", CultureInfo.InvariantCulture),
                Markup.Escape(artifact.BestRouter),
                artifact.BestDeliveryRate.ToString("0.##", CultureInfo.InvariantCulture));
        }

        AnsiConsole.Write(new Rule("Run summary") { Style = new Style(Color.Teal) });
        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine($"[grey]Artifacts directory:[/] {Markup.Escape(outputDirectory)}");
        AnsiConsole.MarkupLine($"[green]Combined HTML report:[/] {Markup.Escape(combinedReportPath)}");
    }

    /// <summary>
    /// Prints available scenarios.
    /// </summary>
    private static void RenderScenarioList(IReadOnlyList<BenchmarkScenario> scenarios)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("Slug");
        table.AddColumn("Name");
        table.AddColumn("Description");

        foreach (var scenario in scenarios)
            table.AddRow(scenario.Slug, scenario.Name, scenario.Description);

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Prints CLI usage information.
    /// </summary>
    private static void RenderHelp()
    {
        AnsiConsole.MarkupLine("[bold]ConsoleUI benchmark runner[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Usage:");
        AnsiConsole.MarkupLine(Markup.Escape("  dotnet run --project ConsoleUI/ConsoleUI.csproj -- [options]"));
        AnsiConsole.MarkupLine(Markup.Escape("  dotnet run --project ConsoleUI/ConsoleUI.csproj"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Options:");
        AnsiConsole.MarkupLine(Markup.Escape("  --scenario, -s <slug[,slug2]>   Scenario slug(s) or 'all' (default)"));
        AnsiConsole.MarkupLine(Markup.Escape("  --output, -o <path>             Output directory for JSON and HTML artifacts"));
        AnsiConsole.MarkupLine(Markup.Escape("  --interactive, -i               Launch interactive menu mode"));
        AnsiConsole.MarkupLine(Markup.Escape("  --topologies <list>             Topology sweep list: fullmesh,mst,k3,k10 or all"));
        AnsiConsole.MarkupLine(Markup.Escape("  --seed-count <n>                Run each scenario on N seeds (>= 1)"));
        AnsiConsole.MarkupLine(Markup.Escape("  --seed-start <n>                First seed for seed sweep (default 1)"));
        AnsiConsole.MarkupLine(Markup.Escape("  --seed-step <n>                 Seed increment between runs (default 1)"));
        AnsiConsole.MarkupLine(Markup.Escape("  --parallelism, -p <n>           Max benchmark workers (default: CPU core count)"));
        AnsiConsole.MarkupLine(Markup.Escape("  --list                          List available scenarios"));
        AnsiConsole.MarkupLine(Markup.Escape("  --help, -h                      Show help"));
    }

    /// <summary>
    /// Parsed command-line options.
    /// </summary>
    private sealed record CommandLineOptions(
        string ScenarioSelector,
        string OutputDirectory,
        bool Interactive,
        IReadOnlyList<ScenarioTopologyProfile> TopologyProfiles,
        int? SeedCount,
        int? SeedStart,
        int? SeedStep,
        int? Parallelism,
        bool ListOnly,
        bool ShowHelp)
    {
        /// <summary>
        /// Parses a CLI argument vector into option values.
        /// </summary>
        public static CommandLineOptions Parse(IReadOnlyList<string> args)
        {
            var selector = "all";
            var output = Path.Combine(Environment.CurrentDirectory, "benchmark-artifacts");
            var interactive = false;
            IReadOnlyList<ScenarioTopologyProfile> topologies = [];
            int? seedCount = null;
            int? seedStart = null;
            int? seedStep = null;
            int? parallelism = null;
            var listOnly = false;
            var showHelp = false;

            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                switch (arg)
                {
                    case "--scenario":
                    case "-s":
                        if (i + 1 < args.Count)
                            selector = args[++i];
                        break;

                    case "--output":
                    case "-o":
                        if (i + 1 < args.Count)
                            output = args[++i];
                        break;

                    case "--list":
                        listOnly = true;
                        break;

                    case "--interactive":
                    case "-i":
                        interactive = true;
                        break;

                    case "--topologies":
                        if (i + 1 < args.Count)
                            topologies = ParseTopologyProfiles(args[++i]);
                        break;

                    case "--seed-count":
                        if (i + 1 < args.Count && int.TryParse(args[++i], out var parsedSeedCount))
                            seedCount = Math.Max(1, parsedSeedCount);
                        break;

                    case "--seed-start":
                        if (i + 1 < args.Count && int.TryParse(args[++i], out var parsedSeedStart))
                            seedStart = Math.Max(1, parsedSeedStart);
                        break;

                    case "--seed-step":
                        if (i + 1 < args.Count && int.TryParse(args[++i], out var parsedSeedStep))
                            seedStep = Math.Max(1, parsedSeedStep);
                        break;

                    case "--parallelism":
                    case "-p":
                        if (i + 1 < args.Count && int.TryParse(args[++i], out var parsedParallelism))
                            parallelism = Math.Max(1, parsedParallelism);
                        break;

                    case "--help":
                    case "-h":
                        showHelp = true;
                        break;
                }
            }

            return new CommandLineOptions(
                selector,
                output,
                interactive,
                topologies,
                seedCount,
                seedStart,
                seedStep,
                parallelism,
                listOnly,
                showHelp);
        }
    }

    /// <summary>
    /// Final artifact metadata for one completed scenario.
    /// </summary>
    private sealed record RunArtifact(
        BenchmarkScenario Scenario,
        BenchmarkSession Session,
        string ScenarioName,
        TimeSpan Duration,
        string BestRouter,
        double BestDeliveryRate,
        string JsonPath);
}
