namespace WebApp.Client.Services;

public class SimulationConfig
{
    public int TickIntervalMs     { get; set; } = 300;
    public int DefaultTTL         { get; set; } = 10;
    public int TicksToTravel      { get; set; } = 3;
    public int VisibilityDistance { get; set; } = 200;

    /// <summary>
    /// Maximum number of simultaneously in-flight packets.
    /// <c>0</c> means automatic: <c>device count ? 2000</c>.
    /// A positive value overrides the automatic limit.
    /// </summary>
    public int MaxActivePackets { get; set; } = 0;
}
