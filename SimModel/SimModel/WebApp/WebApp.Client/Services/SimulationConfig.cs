using Engine.Routers;

namespace WebApp.Client.Services;

/// <summary>
/// Holds all user-configurable simulation settings shown in the Settings panel.
/// <para>
/// Instances are applied to <see cref="Engine.Core.SimulationEngine"/> via
/// <see cref="SimulationService.ApplyConfig"/>. The default values match the
/// engine constants (<see cref="Engine.Core.SimulationEngine.VISIBILITY_DISTANCE"/>,
/// <see cref="Engine.Packets.Packet.TicksToTravel"/>, etc.).
/// </para>
/// </summary>
public class SimulationConfig
{
    /// <summary>Wall-clock delay between successive engine ticks in milliseconds.</summary>
    public int TickIntervalMs { get; set; } = 300;

    /// <summary>
    /// Default TTL assigned to newly created packets.
    /// Maps to <see cref="Engine.Packets.Packet.TTL"/> on construction.
    /// </summary>
    public int DefaultTTL { get; set; } = 10;

    /// <summary>
    /// Number of engine ticks a packet takes to travel between adjacent devices.
    /// Maps to <see cref="Engine.Packets.Packet.TicksToTravel"/>.
    /// </summary>
    public int TicksToTravel { get; set; } = 3;

    /// <summary>
    /// Euclidean radio range; two devices within this distance can see each other.
    /// Applied to <see cref="Engine.Core.SimulationEngine.VisibilityDistance"/> on
    /// <see cref="SimulationService.ApplyConfig"/>.
    /// </summary>
    public int VisibilityDistance { get; set; } = 200;

    /// <summary>
    /// Maximum number of simultaneously in-flight packets.
    /// <c>0</c> means automatic: <c>device count ? 2000</c>.
    /// A positive value overrides the automatic limit.
    /// </summary>
    public int MaxActivePackets { get; set; } = 0;

    /// <summary>
    /// The routing strategy selected in the UI.
    /// Applied to <see cref="Engine.Core.SimulationEngine.Router"/> on
    /// <see cref="SimulationService.ApplyConfig"/>.
    /// </summary>
    public IPacketRouter SelectedRouter { get; set; } = new SmartFloodingPacketRouter();

    /// <summary>
    /// All available routing strategies shown in the settings dropdown.
    /// Order determines display order in the UI.
    /// </summary>
    public static readonly IReadOnlyList<IPacketRouter> AvailableRouters =
    [
        new SmartFloodingPacketRouter(),
        new FloodingPacketRouter(),
    ];
}
