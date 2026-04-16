using Engine.Benchmark;
using Engine.Routers;
using System.Numerics;
using System.Text;

namespace ConsoleUI;

/// <summary>
/// Immutable scenario definition used by the console benchmark runner.
/// </summary>
/// <param name="Slug">Stable id used in file names and CLI selection.</param>
/// <param name="Name">Human-friendly title shown in console output.</param>
/// <param name="Description">Detailed scenario intent and provenance.</param>
/// <param name="Config">Benchmark configuration consumed by BenchmarkRunner.</param>
/// <param name="Vector">Swarm protocol parameter vector applied before each run.</param>
/// <param name="TopologyBuilder">Topology builder used for all routers in this scenario.</param>
/// <param name="SeedTemplate">
/// Optional template used to regenerate this scenario with alternative seeds.
/// </param>
internal sealed record BenchmarkScenario(
    string Slug,
    string Name,
    string Description,
    BenchmarkConfig Config,
    SwarmProtocolVector Vector,
    INetworkBuilder TopologyBuilder,
    GeneratedScenarioDefinition? SeedTemplate = null);

/// <summary>
/// Topology profile used by generated benchmark cases.
/// </summary>
internal enum ScenarioTopologyProfile
{
    /// <summary>
    /// Connect every visible node pair.
    /// </summary>
    FullMesh,

    /// <summary>
    /// Build a visibility-constrained minimum spanning tree.
    /// </summary>
    VisibilityMst,

    /// <summary>
    /// Build a visibility-constrained k-nearest graph with k=3.
    /// </summary>
    KNearest3,

    /// <summary>
    /// Build a visibility-constrained k-nearest graph with k=10.
    /// </summary>
    KNearest10,
}

/// <summary>
/// Vector profile used by generated benchmark cases.
/// </summary>
internal enum ScenarioVectorProfile
{
    /// <summary>
    /// Baseline fixed vector from protocol search artifacts.
    /// </summary>
    BaselineFixed,

    /// <summary>
    /// Convergence-tuned vector with stronger hysteresis.
    /// </summary>
    ConvergenceTuned,
}

/// <summary>
/// User-provided settings for generated benchmark cases.
/// </summary>
internal sealed class GeneratedScenarioDefinition
{
    /// <summary>
    /// Scenario title.
    /// </summary>
    public string Name { get; set; } = "Generated Case";

    /// <summary>
    /// Scenario description.
    /// </summary>
    public string Description { get; set; } = "User-generated benchmark case.";

    /// <summary>
    /// Total node count including hub.
    /// </summary>
    public int TotalNodes { get; set; } = 48;

    /// <summary>
    /// Radio visibility distance.
    /// </summary>
    public int VisibilityDistance { get; set; } = 210;

    /// <summary>
    /// Packet default TTL.
    /// </summary>
    public int DefaultTtl { get; set; } = 12;

    /// <summary>
    /// Packet default travel ticks.
    /// </summary>
    public long TicksToTravel { get; set; } = 3;

    /// <summary>
    /// Tick duration of the scenario.
    /// </summary>
    public long DurationTicks { get; set; } = 320;

    /// <summary>
    /// Share of emitter devices in [0, 1].
    /// </summary>
    public double EmitterShare { get; set; } = 0.40;

    /// <summary>
    /// Min generator emission period in ticks.
    /// </summary>
    public int GeneratorMinTicks { get; set; } = 18;

    /// <summary>
    /// Max generator emission period in ticks.
    /// </summary>
    public int GeneratorMaxTicks { get; set; } = 84;

    /// <summary>
    /// Min emitter control period in ticks.
    /// </summary>
    public int EmitterMinTicks { get; set; } = 12;

    /// <summary>
    /// Max emitter control period in ticks.
    /// </summary>
    public int EmitterMaxTicks { get; set; } = 96;

    /// <summary>
    /// Minimum number of generated events.
    /// </summary>
    public int MinEvents { get; set; } = 14;

    /// <summary>
    /// Maximum number of generated events.
    /// </summary>
    public int MaxEvents { get; set; } = 22;

    /// <summary>
    /// Deterministic random seed.
    /// </summary>
    public int Seed { get; set; } = 4242;

    /// <summary>
    /// Vector profile to apply.
    /// </summary>
    public ScenarioVectorProfile VectorProfile { get; set; } = ScenarioVectorProfile.BaselineFixed;

    /// <summary>
    /// Topology profile to apply.
    /// </summary>
    public ScenarioTopologyProfile TopologyProfile { get; set; } = ScenarioTopologyProfile.KNearest10;

    /// <summary>
    /// Router names to benchmark.
    /// </summary>
    public List<string> RouterNames { get; set; } = [];

    /// <summary>
    /// Optional explicit swarm vector. When set, it overrides
    /// <see cref="VectorProfile"/>.
    /// </summary>
    public SwarmProtocolVector? CustomVector { get; set; }

    /// <summary>
    /// Creates a deep copy of this definition.
    /// </summary>
    /// <returns>Copied definition instance.</returns>
    public GeneratedScenarioDefinition Clone()
    {
        return new GeneratedScenarioDefinition
        {
            Name = Name,
            Description = Description,
            TotalNodes = TotalNodes,
            VisibilityDistance = VisibilityDistance,
            DefaultTtl = DefaultTtl,
            TicksToTravel = TicksToTravel,
            DurationTicks = DurationTicks,
            EmitterShare = EmitterShare,
            GeneratorMinTicks = GeneratorMinTicks,
            GeneratorMaxTicks = GeneratorMaxTicks,
            EmitterMinTicks = EmitterMinTicks,
            EmitterMaxTicks = EmitterMaxTicks,
            MinEvents = MinEvents,
            MaxEvents = MaxEvents,
            Seed = Seed,
            VectorProfile = VectorProfile,
            TopologyProfile = TopologyProfile,
            RouterNames = RouterNames.ToList(),
            CustomVector = CustomVector?.Clone(),
        };
    }
}

/// <summary>
/// Builds benchmark scenarios that mirror the protocol documentation and
/// baseline Python batch-model conditions.
/// </summary>
internal static class BenchmarkScenarioFactory
{
    private static readonly IReadOnlyList<string> DefaultRouterNames =
    [
        new SwarmProtocolPacketRouter().Name,
        new SmartFloodingPacketRouter().Name,
        new FloodingPacketRouter().Name,
    ];

    /// <summary>
    /// Creates default generation settings used by interactive mode.
    /// </summary>
    /// <returns>Default generated scenario definition.</returns>
    public static GeneratedScenarioDefinition CreateDefaultGeneratedDefinition()
    {
        return new GeneratedScenarioDefinition
        {
            TopologyProfile = ScenarioTopologyProfile.KNearest10,
            RouterNames = DefaultRouterNames.ToList(),
        };
    }

    /// <summary>
    /// Creates a seed variant of an existing scenario while keeping all
    /// non-random parameters unchanged.
    /// </summary>
    /// <param name="scenario">Source scenario parameters.</param>
    /// <param name="seed">Deterministic seed for the variant.</param>
    /// <returns>Seed variant scenario.</returns>
    public static BenchmarkScenario CreateSeedVariant(BenchmarkScenario scenario, int seed)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var definition = CreateDefinitionFromScenario(scenario, seed);
        var slug = Slugify($"{scenario.Slug}-seed-{seed}");
        var name = $"{scenario.Name} [seed {seed}]";
        var description = string.IsNullOrWhiteSpace(scenario.Description)
            ? $"Seed variant {seed}."
            : $"{scenario.Description} Seed variant {seed} with regenerated topology and events.";

        return CreateGeneratedScenario(
            definition,
            slugOverride: slug,
            nameOverride: name,
            descriptionOverride: description);
    }

    /// <summary>
    /// Creates a generated-definition snapshot from an existing scenario.
    /// </summary>
    /// <param name="scenario">Source scenario.</param>
    /// <param name="seedOverride">
    /// Optional seed override; when null or &lt;= 0 the source/template seed is used.
    /// </param>
    /// <returns>Definition suitable for regenerated seed variants.</returns>
    public static GeneratedScenarioDefinition CreateDefinitionFromScenario(
        BenchmarkScenario scenario,
        int? seedOverride = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        GeneratedScenarioDefinition definition;

        if (scenario.SeedTemplate is not null)
        {
            definition = scenario.SeedTemplate.Clone();
        }
        else
        {
            var nonHubDevices = scenario.Config.Devices
                .Where(d => d.Kind != BenchmarkDeviceKind.Hub)
                .ToList();

            var emitters = nonHubDevices.Where(d => d.Kind == BenchmarkDeviceKind.Emitter).ToList();
            var generators = nonHubDevices.Where(d => d.Kind == BenchmarkDeviceKind.Generator).ToList();

            definition = new GeneratedScenarioDefinition
            {
                Name = scenario.Name,
                Description = scenario.Description,
                TotalNodes = Math.Max(2, scenario.Config.Devices.Count),
                VisibilityDistance = scenario.Config.VisibilityDistance,
                DefaultTtl = scenario.Config.DefaultTtl,
                TicksToTravel = scenario.Config.TicksToTravel,
                DurationTicks = scenario.Config.DurationTicks,
                EmitterShare = nonHubDevices.Count == 0
                    ? 0.40
                    : (double)emitters.Count / nonHubDevices.Count,
                GeneratorMinTicks = generators.Count == 0
                    ? 18
                    : (int)Math.Round((double)generators.Min(d => d.GenFrequencyTicks), MidpointRounding.AwayFromZero),
                GeneratorMaxTicks = generators.Count == 0
                    ? 84
                    : (int)Math.Round((double)generators.Max(d => d.GenFrequencyTicks), MidpointRounding.AwayFromZero),
                EmitterMinTicks = emitters.Count == 0
                    ? 12
                    : (int)Math.Round((double)emitters.Min(d => d.ControlFrequencyTicks), MidpointRounding.AwayFromZero),
                EmitterMaxTicks = emitters.Count == 0
                    ? 96
                    : (int)Math.Round((double)emitters.Max(d => d.ControlFrequencyTicks), MidpointRounding.AwayFromZero),
                MinEvents = scenario.Config.Events.Count,
                MaxEvents = scenario.Config.Events.Count,
                Seed = 1,
                VectorProfile = InferVectorProfile(scenario.Vector),
                TopologyProfile = InferTopologyProfile(scenario.TopologyBuilder),
                RouterNames = scenario.Config.RouterNames.ToList(),
                CustomVector = scenario.Vector.Clone(),
            };
        }

        if (seedOverride.HasValue && seedOverride.Value > 0)
            definition.Seed = seedOverride.Value;

        if (definition.Seed <= 0)
            definition.Seed = 1;

        return definition;
    }

    /// <summary>
    /// Builds one generated scenario from user-provided settings.
    /// </summary>
    /// <param name="definition">Generated scenario settings.</param>
    /// <param name="slugOverride">Optional explicit scenario slug.</param>
    /// <param name="nameOverride">Optional explicit scenario name.</param>
    /// <param name="descriptionOverride">Optional explicit scenario description.</param>
    /// <returns>Ready-to-run benchmark scenario.</returns>
    public static BenchmarkScenario CreateGeneratedScenario(
        GeneratedScenarioDefinition definition,
        string? slugOverride = null,
        string? nameOverride = null,
        string? descriptionOverride = null)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var normalized = definition.Clone();

        var normalizedSeed = normalized.Seed <= 0
            ? Math.Abs(Environment.TickCount)
            : normalized.Seed;

        normalized.Seed = normalizedSeed;

        var name = !string.IsNullOrWhiteSpace(nameOverride)
            ? nameOverride.Trim()
            : string.IsNullOrWhiteSpace(normalized.Name)
            ? "Generated Case"
            : normalized.Name.Trim();

        var description = !string.IsNullOrWhiteSpace(descriptionOverride)
            ? descriptionOverride.Trim()
            : string.IsNullOrWhiteSpace(normalized.Description)
            ? "User-generated benchmark case."
            : normalized.Description.Trim();

        normalized.Name = name;
        normalized.Description = description;

        var topologyBuilder = CreateTopologyBuilder(normalized.TopologyProfile);
        var vector = normalized.CustomVector?.Normalized() ?? CreateVector(normalized.VectorProfile);

        var routerNames = normalized.RouterNames
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (routerNames.Count == 0)
            routerNames = DefaultRouterNames.ToList();

        var nodeCount = Math.Clamp(normalized.TotalNodes, 8, 240);
        var visibilityDistance = Math.Clamp(normalized.VisibilityDistance, 80, 900);
        var defaultTtl = Math.Clamp(normalized.DefaultTtl, 4, 64);
        var ticksToTravel = (int)Math.Clamp(normalized.TicksToTravel, 1, 40);
        var durationTicks = Math.Clamp(normalized.DurationTicks, 120, 20_000);
        var minEvents = Math.Clamp(normalized.MinEvents, 1, 500);
        var maxEvents = Math.Clamp(normalized.MaxEvents, minEvents, 500);

        var config = new BenchmarkConfig
        {
            Name = name,
            Description = description,
            VisibilityDistance = visibilityDistance,
            DefaultTtl = defaultTtl,
            TicksToTravel = ticksToTravel,
            MaxActivePackets = 0,
            DurationTicks = durationTicks,
            Devices = BuildConnectedDevices(
                totalNodes: nodeCount,
                visibilityDistance: visibilityDistance,
                emitterShare: Math.Clamp(normalized.EmitterShare, 0.0, 1.0),
                generatorRange: (normalized.GeneratorMinTicks, normalized.GeneratorMaxTicks),
                emitterRange: (normalized.EmitterMinTicks, normalized.EmitterMaxTicks),
                seed: normalizedSeed),
            RouterNames = routerNames,
        };

        config.Events = BuildOperationalEvents(
            initialDevices: config.Devices,
            visibilityDistance: config.VisibilityDistance,
            durationTicks: config.DurationTicks,
            seed: normalizedSeed + 100_003,
            minEvents: minEvents,
            maxEvents: maxEvents,
            joinNamePrefix: "GeneratedJoin");

        var slug = string.IsNullOrWhiteSpace(slugOverride)
            ? Slugify($"generated-{name}-{normalizedSeed}")
            : Slugify(slugOverride.Trim());

        return new BenchmarkScenario(
            Slug: slug,
            Name: name,
            Description: description,
            Config: config,
            Vector: vector,
            TopologyBuilder: topologyBuilder,
            SeedTemplate: normalized.Clone());
    }

    /// <summary>
    /// Creates all built-in realistic benchmark scenarios.
    /// </summary>
    /// <returns>Ready-to-run scenario list.</returns>
    public static IReadOnlyList<BenchmarkScenario> CreateScenarios()
    {
        return
        [
            CreateBaselineFixedVectorScenario(),
            CreateConvergenceStressScenario(),
        ];
    }

    /// <summary>
    /// Detects topology profile from scenario topology builder.
    /// </summary>
    /// <param name="scenario">Scenario to inspect.</param>
    /// <returns>Detected topology profile.</returns>
    public static ScenarioTopologyProfile DetectTopologyProfile(BenchmarkScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return InferTopologyProfile(scenario.TopologyBuilder);
    }

    /// <summary>
    /// Scenario based on Protocol/_theoreme_ai_search/try_3_baseline/
    /// request_baseline_fixed_vector.json.
    /// </summary>
    private static BenchmarkScenario CreateBaselineFixedVectorScenario()
    {
        var definition = new GeneratedScenarioDefinition
        {
            Name = "Baseline fixed vector",
            Description = "Protocol baseline conditions from try_3_baseline with medium mesh size.",
            VisibilityDistance = 210,
            DefaultTtl = 12,
            TicksToTravel = 3,
            DurationTicks = 320,
            TotalNodes = 48,
            EmitterShare = 0.38,
            GeneratorMinTicks = 18,
            GeneratorMaxTicks = 84,
            EmitterMinTicks = 12,
            EmitterMaxTicks = 96,
            MinEvents = 12,
            MaxEvents = 18,
            Seed = 42,
            VectorProfile = ScenarioVectorProfile.BaselineFixed,
            TopologyProfile = ScenarioTopologyProfile.FullMesh,
            RouterNames = DefaultRouterNames.ToList(),
        };

        definition.TopologyProfile = ScenarioTopologyProfile.KNearest10;

        return CreateGeneratedScenario(
            definition,
            slugOverride: "baseline-fixed-vector",
            nameOverride: "Baseline Fixed Vector (48 nodes)",
            descriptionOverride: "Reference medium mesh profile from the baseline vector request with deterministic random generation and realistic joins/removals/toggles.");
    }

    /// <summary>
    /// Scenario based on convergence tuning guidance with stronger churn and
    /// denser topology.
    /// </summary>
    private static BenchmarkScenario CreateConvergenceStressScenario()
    {
        var definition = new GeneratedScenarioDefinition
        {
            Name = "Convergence stress",
            Description = "Convergence tuning profile with denser mesh and heavier event churn.",
            VisibilityDistance = 230,
            DefaultTtl = 14,
            TicksToTravel = 3,
            DurationTicks = 420,
            TotalNodes = 64,
            EmitterShare = 0.42,
            GeneratorMinTicks = 16,
            GeneratorMaxTicks = 72,
            EmitterMinTicks = 10,
            EmitterMaxTicks = 80,
            MinEvents = 18,
            MaxEvents = 28,
            Seed = 77,
            VectorProfile = ScenarioVectorProfile.ConvergenceTuned,
            TopologyProfile = ScenarioTopologyProfile.FullMesh,
            RouterNames = DefaultRouterNames.ToList(),
        };

        definition.TopologyProfile = ScenarioTopologyProfile.KNearest10;

        return CreateGeneratedScenario(
            definition,
            slugOverride: "convergence-stress",
            nameOverride: "Convergence Stress (64 nodes)",
            descriptionOverride: "Tuning-guide profile with stable decay and stronger topology churn on the same visibility graph as the baseline case.");
    }

    private static ScenarioTopologyProfile InferTopologyProfile(INetworkBuilder builder)
    {
        return builder switch
        {
            MinimumSpanningTreeNetworkBuilder => ScenarioTopologyProfile.VisibilityMst,
            KNearestNetworkBuilder kBuilder when kBuilder.MaxDegree >= 10 => ScenarioTopologyProfile.KNearest10,
            KNearestNetworkBuilder => ScenarioTopologyProfile.KNearest3,
            _ => ScenarioTopologyProfile.FullMesh,
        };
    }

    private static ScenarioVectorProfile InferVectorProfile(SwarmProtocolVector vector)
    {
        var convergence = CreateVector(ScenarioVectorProfile.ConvergenceTuned);

        var isConvergence = vector.QForward == convergence.QForward
            && vector.RootSourceCharge == convergence.RootSourceCharge
            && vector.SwitchHysteresis == convergence.SwitchHysteresis
            && Math.Abs(vector.SwitchHysteresisRatio - convergence.SwitchHysteresisRatio) < 0.00001
            && vector.ParentDeadTicks == convergence.ParentDeadTicks
            && Math.Abs(vector.DecayPercent - convergence.DecayPercent) < 0.00001;

        return isConvergence
            ? ScenarioVectorProfile.ConvergenceTuned
            : ScenarioVectorProfile.BaselineFixed;
    }

    private static SwarmProtocolVector CreateVector(ScenarioVectorProfile profile)
    {
        return profile switch
        {
            ScenarioVectorProfile.ConvergenceTuned => new SwarmProtocolVector
            {
                QForward = 194,
                RootSourceCharge = 1500,
                PenaltyLambda = 28,
                SwitchHysteresis = 15,
                SwitchHysteresisRatio = 0.03,
                ParentDeadTicks = 140,
                ChargeDropPerHop = 80,
                ChargeSpreadFactor = 0.28,
                DecayIntervalSteps = 60,
                DecayPercent = 0.12,
                LinkMemory = 0.94,
                LinkLearningRate = 0.20,
                LinkBonusMax = 45,
            },
            _ => new SwarmProtocolVector
            {
                QForward = 194,
                RootSourceCharge = 1500,
                PenaltyLambda = 28,
                SwitchHysteresis = 9,
                SwitchHysteresisRatio = 0.07,
                ParentDeadTicks = 120,
                ChargeDropPerHop = 80,
                ChargeSpreadFactor = 0.28,
                DecayIntervalSteps = 60,
                DecayPercent = 0.13,
                LinkMemory = 0.912,
                LinkLearningRate = 0.23,
                LinkBonusMax = 60,
            },
        };
    }

    private static INetworkBuilder CreateTopologyBuilder(ScenarioTopologyProfile profile)
    {
        return profile switch
        {
            ScenarioTopologyProfile.VisibilityMst => new MinimumSpanningTreeNetworkBuilder(),
            ScenarioTopologyProfile.KNearest3 => new KNearestNetworkBuilder(3),
            ScenarioTopologyProfile.KNearest10 => new KNearestNetworkBuilder(10),
            _ => new FullMeshNetworkBuilder(),
        };
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "generated-case";

        var builder = new StringBuilder(value.Length);
        var lastDash = false;

        foreach (var raw in value.Trim().ToLowerInvariant())
        {
            if ((raw >= 'a' && raw <= 'z') || (raw >= '0' && raw <= '9'))
            {
                builder.Append(raw);
                lastDash = false;
                continue;
            }

            if (lastDash)
                continue;

            builder.Append('-');
            lastDash = true;
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "generated-case" : slug;
    }

    /// <summary>
    /// Creates a connected initial device set with one hub and mixed generators
    /// and emitters.
    /// </summary>
    private static List<DeviceBenchmarkDto> BuildConnectedDevices(
        int totalNodes,
        int visibilityDistance,
        double emitterShare,
        (int Min, int Max) generatorRange,
        (int Min, int Max) emitterRange,
        int seed)
    {
        var rng = new Random(seed);
        var nodes = Math.Clamp(totalNodes, 2, 220);
        var emitShare = Math.Clamp(emitterShare, 0.0, 1.0);

        var genMin = Math.Clamp(Math.Min(generatorRange.Min, generatorRange.Max), 1, 10000);
        var genMax = Math.Clamp(Math.Max(generatorRange.Min, generatorRange.Max), genMin, 10000);

        var emitMin = Math.Clamp(Math.Min(emitterRange.Min, emitterRange.Max), 1, 10000);
        var emitMax = Math.Clamp(Math.Max(emitterRange.Min, emitterRange.Max), emitMin, 10000);

        var devices = new List<DeviceBenchmarkDto>
        {
            new()
            {
                Name = "Hub",
                Kind = BenchmarkDeviceKind.Hub,
                X = 0,
                Y = 0,
            },
        };

        var anchors = new List<Vector2> { Vector2.Zero };
        var sensorIndex = 1;
        var lampIndex = 1;

        for (var i = 1; i < nodes; i++)
        {
            var pos = CreateConnectedPoint(anchors, visibilityDistance, rng, minFactor: 0.24, maxFactor: 0.84);
            anchors.Add(pos);

            if (rng.NextDouble() < emitShare)
            {
                devices.Add(new DeviceBenchmarkDto
                {
                    Name = $"Lamp-{lampIndex++}",
                    Kind = BenchmarkDeviceKind.Emitter,
                    X = pos.X,
                    Y = pos.Y,
                    ControlFrequencyTicks = rng.Next(emitMin, emitMax + 1),
                });
            }
            else
            {
                devices.Add(new DeviceBenchmarkDto
                {
                    Name = $"Sensor-{sensorIndex++}",
                    Kind = BenchmarkDeviceKind.Generator,
                    X = pos.X,
                    Y = pos.Y,
                    GenFrequencyTicks = rng.Next(genMin, genMax + 1),
                });
            }
        }

        return devices;
    }

    /// <summary>
    /// Creates a realistic mixed event timeline with toggles, removals, and
    /// mid-run device joins.
    /// </summary>
    private static List<BenchmarkEventEntry> BuildOperationalEvents(
        IReadOnlyList<DeviceBenchmarkDto> initialDevices,
        int visibilityDistance,
        long durationTicks,
        int seed,
        int minEvents,
        int maxEvents,
        string joinNamePrefix)
    {
        var rng = new Random(seed);
        var events = new List<BenchmarkEventEntry>();

        var knownKinds = initialDevices
            .ToDictionary(x => x.Name, x => x.Kind, StringComparer.Ordinal);

        var anchors = initialDevices
            .Select(x => new Vector2(x.X, x.Y))
            .ToList();

        var joinIndex = 1;
        var eventCount = rng.Next(Math.Max(1, minEvents), Math.Max(minEvents + 1, maxEvents + 1));

        var startTick = 12;
        var endTick = (int)Math.Max(startTick + 2, Math.Min(durationTicks - 1, 100000));

        for (var i = 0; i < eventCount; i++)
        {
            var tick = rng.Next(startTick, endTick);
            var roll = rng.NextDouble();

            var emitterNames = knownKinds
                .Where(x => x.Value == BenchmarkDeviceKind.Emitter)
                .Select(x => x.Key)
                .ToArray();

            var removableNames = knownKinds
                .Where(x => x.Value != BenchmarkDeviceKind.Hub)
                .Select(x => x.Key)
                .ToArray();

            BenchmarkEvent evt;

            if (roll < 0.50 && emitterNames.Length > 0)
            {
                var target = emitterNames[rng.Next(emitterNames.Length)];
                evt = new ToggleBenchmarkEvent(target);
            }
            else if (roll < 0.72 && removableNames.Length > 0)
            {
                var removeTarget = removableNames[rng.Next(removableNames.Length)];
                knownKinds.Remove(removeTarget);
                evt = new RemoveDeviceBenchmarkEvent(removeTarget);
            }
            else
            {
                var joined = CreateJoinDevice(anchors, visibilityDistance, rng, joinNamePrefix, joinIndex++);
                knownKinds[joined.Name] = joined.Kind;
                anchors.Add(new Vector2(joined.X, joined.Y));
                evt = new AddDeviceBenchmarkEvent(joined);
            }

            events.Add(new BenchmarkEventEntry(tick, evt));
        }

        return events
            .OrderBy(e => e.AtTick)
            .ToList();
    }

    /// <summary>
    /// Creates a single joining device near existing nodes to preserve graph
    /// connectivity under realistic churn.
    /// </summary>
    private static DeviceBenchmarkDto CreateJoinDevice(
        IReadOnlyList<Vector2> anchors,
        int visibilityDistance,
        Random rng,
        string joinNamePrefix,
        int joinIndex)
    {
        var pos = CreateConnectedPoint(anchors, visibilityDistance, rng, minFactor: 0.20, maxFactor: 0.86);
        var isEmitter = rng.NextDouble() < 0.40;

        return new DeviceBenchmarkDto
        {
            Name = $"{joinNamePrefix}-{joinIndex:00}",
            Kind = isEmitter ? BenchmarkDeviceKind.Emitter : BenchmarkDeviceKind.Generator,
            X = pos.X,
            Y = pos.Y,
            GenFrequencyTicks = rng.Next(14, 86),
            ControlFrequencyTicks = rng.Next(10, 96),
        };
    }

    /// <summary>
    /// Generates one point within visibility range of an existing anchor.
    /// </summary>
    private static Vector2 CreateConnectedPoint(
        IReadOnlyList<Vector2> anchors,
        int visibilityDistance,
        Random rng,
        double minFactor,
        double maxFactor)
    {
        const int maxAttempts = 64;

        var minRadiusFactor = Math.Clamp(minFactor, 0.05, 0.95);
        var maxRadiusFactor = Math.Clamp(maxFactor, minRadiusFactor, 0.98);

        var minSpacing = visibilityDistance * 0.12f;
        var fallback = Vector2.Zero;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var anchor = anchors[rng.Next(anchors.Count)];
            var angle = rng.NextDouble() * Math.PI * 2.0;
            var radiusFactor = minRadiusFactor + rng.NextDouble() * (maxRadiusFactor - minRadiusFactor);
            var radius = (float)(visibilityDistance * radiusFactor);

            var candidate = anchor + new Vector2(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius));

            if (attempt == 0)
                fallback = candidate;

            var tooClose = false;
            foreach (var point in anchors)
            {
                if (Vector2.Distance(point, candidate) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return candidate;
        }

        return fallback;
    }
}
