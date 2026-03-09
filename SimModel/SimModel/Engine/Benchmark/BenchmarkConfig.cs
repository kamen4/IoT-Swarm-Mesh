namespace Engine.Benchmark;

/// <summary>
/// Fully serialisable description of a benchmark scenario.
/// <para>
/// A <see cref="BenchmarkConfig"/> defines everything needed to reproduce a
/// headless simulation run deterministically:
/// <list type="bullet">
///   <item>Simulation engine settings (visibility, TTL, packet travel time).</item>
///   <item>The initial network layout as a list of <see cref="DeviceBenchmarkDto"/>.</item>
///   <item>A timeline of <see cref="BenchmarkEventEntry"/> items that modify the
///         network at specific ticks (toggle, add, remove device).</item>
///   <item>The total number of ticks to simulate.</item>
///   <item>The list of router names to test; <see cref="BenchmarkRunner"/> will
///         run one full simulation per router and collect results for comparison.</item>
/// </list>
/// </para>
/// <para>
/// Instances are stored as JSON together with their <see cref="BenchmarkSession"/>
/// results so a test can be replayed or shared.
/// </para>
/// </summary>
public sealed class BenchmarkConfig
{
    // ------------------------------------------------------------------
    // Identity
    // ------------------------------------------------------------------

    /// <summary>User-assigned name shown in the UI list.</summary>
    public string Name { get; set; } = "New benchmark";

    /// <summary>Optional free-text description.</summary>
    public string Description { get; set; } = "";

    // ------------------------------------------------------------------
    // Engine settings
    // ------------------------------------------------------------------

    /// <summary>Visibility distance applied to the engine during the run.</summary>
    public int VisibilityDistance { get; set; } = 200;

    /// <summary>Default TTL assigned to new packets.</summary>
    public int DefaultTtl { get; set; } = 10;

    /// <summary>Ticks a packet spends in transit between adjacent nodes.</summary>
    public int TicksToTravel { get; set; } = 3;

    /// <summary>
    /// Maximum simultaneously in-flight packets (0 = auto: devices ? 2000).
    /// </summary>
    public int MaxActivePackets { get; set; } = 0;

    // ------------------------------------------------------------------
    // Simulation duration
    // ------------------------------------------------------------------

    /// <summary>Total number of engine ticks to run per router.</summary>
    public long DurationTicks { get; set; } = 300;

    // ------------------------------------------------------------------
    // Network layout
    // ------------------------------------------------------------------

    /// <summary>
    /// Devices present at the start of the simulation.
    /// Applied in declaration order via <see cref="Engine.Core.SimulationEngine.RegisterDevice"/>.
    /// </summary>
    public List<DeviceBenchmarkDto> Devices { get; set; } = [];

    // ------------------------------------------------------------------
    // Scheduled events
    // ------------------------------------------------------------------

    /// <summary>
    /// Ordered list of events to apply at specific ticks.
    /// Sorted by <see cref="BenchmarkEventEntry.AtTick"/> before the run starts.
    /// </summary>
    public List<BenchmarkEventEntry> Events { get; set; } = [];

    // ------------------------------------------------------------------
    // Protocol selection
    // ------------------------------------------------------------------

    /// <summary>
    /// Names of routers to test, matched against
    /// <see cref="Engine.Routers.IPacketRouter.Name"/>.
    /// The runner iterates this list and executes one full simulation per entry.
    /// </summary>
    public List<string> RouterNames { get; set; } = [];
}
