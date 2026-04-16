namespace WebApp.Client.Services;

/// <summary>
/// User-controlled options for random simulation layout generation.
/// </summary>
public sealed class RandomGenerationOptions
{
    /// <summary>
    /// Number of non-hub devices to generate.
    /// </summary>
    public int DeviceCount { get; set; } = 12;

    /// <summary>
    /// When enabled, every generated node is placed within radio range of an
    /// existing node, which keeps the graph connected.
    /// </summary>
    public bool EnsureConnected { get; set; } = true;

    /// <summary>
    /// Percentage of emitters in generated non-hub devices.
    /// </summary>
    public int EmitterSharePercent { get; set; } = 40;

    /// <summary>
    /// Minimum generator emission period in ticks.
    /// </summary>
    public int GeneratorMinTicks { get; set; } = 20;

    /// <summary>
    /// Maximum generator emission period in ticks.
    /// </summary>
    public int GeneratorMaxTicks { get; set; } = 80;

    /// <summary>
    /// Minimum emitter control period in ticks.
    /// </summary>
    public int EmitterMinTicks { get; set; } = 12;

    /// <summary>
    /// Maximum emitter control period in ticks.
    /// </summary>
    public int EmitterMaxTicks { get; set; } = 90;

    /// <summary>
    /// Minimum normalized radius for connected placement.
    /// </summary>
    public double ConnectedMinRadiusFactor { get; set; } = 0.25;

    /// <summary>
    /// Maximum normalized radius for connected placement.
    /// </summary>
    public double ConnectedMaxRadiusFactor { get; set; } = 0.82;

    /// <summary>
    /// Maximum normalized radius when unconstrained random placement is used.
    /// </summary>
    public double FreeRadiusFactor { get; set; } = 2.5;
}
