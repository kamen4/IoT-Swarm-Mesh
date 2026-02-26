namespace WebApp.Client.Services;

public class SimulationConfig
{
    public int TickIntervalMs     { get; set; } = 300;
    public int DefaultTTL         { get; set; } = 10;
    public int TicksToTravel      { get; set; } = 3;
    public int VisibilityDistance { get; set; } = 200;
}
