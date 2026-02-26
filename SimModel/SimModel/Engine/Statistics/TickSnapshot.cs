namespace Engine.Statistics;

/// <summary>
/// An immutable snapshot of all plottable metric values captured at the end
/// of a single engine tick. Stored in the <see cref="SimulationStatistics"/>
/// history ring-buffer and consumed by the chart UI.
/// </summary>
public sealed record TickSnapshot(
    long   Tick,
    double ActivePackets,
    double TotalDelivered,
    double TotalExpired,
    double DeliveryRate,
    double TickMs
);
