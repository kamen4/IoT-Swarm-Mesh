using Engine.Statistics;

namespace Engine.Benchmark;

/// <summary>
/// The collected statistics from one headless simulation run for a single
/// router protocol.
/// </summary>
public sealed class BenchmarkResult
{
    // ------------------------------------------------------------------
    // Identity
    // ------------------------------------------------------------------

    /// <summary>
    /// <see cref="Engine.Routers.IPacketRouter.Name"/> of the router used for this run.
    /// </summary>
    public string RouterName { get; set; } = "";

    // ------------------------------------------------------------------
    // Final metric values  (snapshot at end-of-run)
    // ------------------------------------------------------------------

    /// <summary>Total packets enqueued (including flood clones).</summary>
    public double TotalPacketsRegistered { get; set; }

    /// <summary>Packets that reached their destination.</summary>
    public double TotalPacketsDelivered  { get; set; }

    /// <summary>Packets dropped because TTL hit zero.</summary>
    public double TotalPacketsExpired    { get; set; }

    /// <summary>Packets delivered more than once via different flood paths.</summary>
    public double DuplicateDeliveries    { get; set; }

    /// <summary>Average number of hops per unique delivered packet.</summary>
    public double AvgHopCount            { get; set; }

    /// <summary>Delivery rate at end of run: Delivered / (Delivered + Expired) ? 100.</summary>
    public double DeliveryRate           { get; set; }

    /// <summary>Average wall-clock ms per engine tick.</summary>
    public double AvgTickMs              { get; set; }

    // ------------------------------------------------------------------
    // Time-series history for charting
    // ------------------------------------------------------------------

    /// <summary>
    /// Per-tick snapshots collected during the run.
    /// Contains at most <see cref="SimulationStatistics.MaxHistoryLength"/> entries
    /// (the last 300 ticks).  Stored as an array for efficient serialisation.
    /// </summary>
    public TickSnapshot[] History { get; set; } = [];
}
