using Engine.Core;
using System.Threading;

namespace Engine.Statistics;

/// <summary>
/// Statistics collector that listens to <see cref="SimulationEngine"/> events
/// and accumulates simulation metrics.
/// <para>
/// Extensibility: add a new <see cref="StatMetric"/> to <see cref="Metrics"/>
/// and subscribe to any <see cref="SimulationEngine"/> event to populate it.
/// The <see cref="Updated"/> event is raised after every change so UI layers
/// can re-render reactively without polling.
/// </para>
/// <para>
/// Every tick a <see cref="TickSnapshot"/> is appended to <see cref="History"/>
/// (capped at <see cref="MaxHistoryLength"/> entries) for time-series charts.
/// </para>
/// </summary>
public sealed class SimulationStatistics
{
    private static readonly SimulationStatistics GlobalInstance = new(SimulationEngine.Instance);
    private static readonly AsyncLocal<SimulationStatistics?> ScopedInstance = new();

    /// <summary>
    /// Gets the current statistics collector instance.
    /// <para>
    /// Default behavior returns the process-global collector.
    /// Benchmark code may temporarily override this per async-flow via
    /// <see cref="PushScopedInstance"/> to isolate metrics for parallel runs.
    /// </para>
    /// </summary>
    public static SimulationStatistics Instance => ScopedInstance.Value ?? GlobalInstance;

    /// <summary>
    /// Creates a detached statistics collector bound to
    /// <paramref name="engine"/>.
    /// </summary>
    /// <param name="engine">Engine instance to observe.</param>
    /// <returns>A new isolated statistics collector.</returns>
    public static SimulationStatistics CreateIsolated(SimulationEngine engine)
        => new(engine);

    /// <summary>
    /// Overrides <see cref="Instance"/> for the current async flow until the
    /// returned scope is disposed.
    /// </summary>
    /// <param name="statistics">Collector to expose as <see cref="Instance"/>.</param>
    /// <returns>A scope token that restores the previous collector on dispose.</returns>
    public static IDisposable PushScopedInstance(SimulationStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        var previous = ScopedInstance.Value;
        ScopedInstance.Value = statistics;
        return new ScopedStatisticsToken(previous);
    }

    /// <summary>Raised after any metric value changes.</summary>
    public event Action? Updated;

    /// <summary>Maximum number of per-tick snapshots retained in <see cref="History"/>.</summary>
    public const int MaxHistoryLength = 300;

    // ?? Metric definitions ????????????????????????????????????????????????????
    // Add isPlottable: true to expose a metric as a selectable chart series.

    /// <summary>Total packets enqueued into the simulation, including every flood clone.</summary>
    public StatMetric TotalPacketsRegistered { get; } = new("Packets registered", "Total packets enqueued (including flood copies)");

    /// <summary>Packets that successfully reached their intended destination device.</summary>
    public StatMetric TotalPacketsDelivered  { get; } = new("Packets delivered",  "Packets that reached their destination",         isPlottable: true);

    /// <summary>Packets discarded because their TTL counter reached zero before delivery.</summary>
    public StatMetric TotalPacketsExpired    { get; } = new("Packets expired",    "Packets dropped because TTL hit zero",           isPlottable: true);

    /// <summary>Cumulative number of devices registered in the engine since the last reset.</summary>
    public StatMetric TotalDevicesAdded      { get; } = new("Devices added",      "Cumulative device registrations");

    /// <summary>Total engine ticks elapsed since the last reset.</summary>
    public StatMetric TotalTicks             { get; } = new("Total ticks",        "Engine ticks elapsed since last reset");

    /// <summary>Running arithmetic mean of wall-clock milliseconds spent per <see cref="SimulationEngine.Tick"/> call.</summary>
    public StatMetric AvgTickMs              { get; } = new("Avg tick (ms)",      "Running average wall-clock time per tick",       isDecimal: true, isPlottable: true);

    /// <summary>Number of packets currently in-flight in the simulation priority queue.</summary>
    public StatMetric ActivePackets          { get; } = new("Active packets",     "Packets currently in-flight",                    isPlottable: true);

    /// <summary>
    /// Ratio of delivered packets to the total of delivered and expired packets, expressed as a percentage.
    /// Computed as <c>TotalPacketsDelivered / (TotalPacketsDelivered + TotalPacketsExpired) ? 100</c>.
    /// </summary>
    public StatMetric DeliveryRate { get; } = new("Delivery rate (%)", "Delivered / (Delivered + Expired) ? 100", isDecimal: true, isPlottable: true);

    /// <summary>
    /// Number of duplicate deliveries: packets whose <see cref="Engine.Packets.Packet.OriginId"/>
    /// had already been delivered before via a different flood clone.
    /// High values indicate that the active routing strategy produces excessive
    /// redundant paths, or that TTL is set too high for the current topology.
    /// </summary>
    public StatMetric DuplicateDeliveries { get; } = new("Duplicate deliveries", "Packets delivered more than once (different flood clones of the same origin)", isPlottable: true);

    /// <summary>
    /// Running arithmetic mean of the number of hops a packet travels from its
    /// originating device to the destination.
    /// Computed as <c>InitialTtl ? TTL</c> at the moment of delivery and averaged
    /// over all unique (first-arrival) deliveries.
    /// </summary>
    public StatMetric AvgHopCount { get; } = new("Avg hop count", "Average number of hops per delivered packet (first delivery only)", isDecimal: true, isPlottable: true);

    /// <summary>All metrics in declaration order  -  drives the summary card grid.</summary>
    public IReadOnlyList<StatMetric> Metrics { get; }

    /// <summary>Metrics that can be selected as a chart series.</summary>
    public IReadOnlyList<StatMetric> PlottableMetrics { get; }

    /// <summary>
    /// Per-tick snapshots in chronological order, capped at
    /// <see cref="MaxHistoryLength"/>.
    /// </summary>
    public IReadOnlyCollection<TickSnapshot> History => _history;

    // Queue gives O(1) Enqueue and Dequeue (vs List.RemoveAt(0) which is O(N)).
    private readonly Queue<TickSnapshot> _history = new();
    private readonly SimulationEngine _engine;
    private double _tickMsAccumulator;
    private long   _tickMsSamples;
    // The very first dt sample is unreliable: SimulationEngine stamps _lastTickTime
    // at construction, not at the first Tick() call, so the gap can be arbitrarily
    // large (however long the user spends before pressing Start).
    // We discard tick 1 from the average by only accumulating from tick 2 onward.
    private bool _firstTickSeen;

    // Tracks OriginIds that have already been delivered at least once.
    // Used to detect duplicate deliveries from flood clones.
    private readonly HashSet<Guid> _deliveredOrigins = new();

    // Accumulator for average hop count  -  only first-delivery hops are counted.
    private double _hopAccumulator;
    private long   _hopSamples;

    private SimulationStatistics(SimulationEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        _engine = engine;

        Metrics =
        [
            TotalPacketsRegistered,
            TotalPacketsDelivered,
            TotalPacketsExpired,
            TotalDevicesAdded,
            TotalTicks,
            AvgTickMs,
            ActivePackets,
            DeliveryRate,
            DuplicateDeliveries,
            AvgHopCount,
        ];

        PlottableMetrics = Metrics.Where(m => m.IsPlottable).ToList();

        _engine.PacketRegistered += (_, _)  =>
        {
            TotalPacketsRegistered.Increment();
            RefreshDerived();
            // No Notify() here  -  UI is refreshed once per tick via the Ticked handler.
        };
        _engine.PacketDelivered  += (_, e)  =>
        {
            TotalPacketsDelivered.Increment();

            if (!_deliveredOrigins.Add(e.Packet.OriginId))
            {
                // OriginId already seen  -  this is a duplicate delivery from a different flood clone.
                DuplicateDeliveries.Increment();
            }
            else
            {
                // First delivery of this logical packet  -  record its hop count.
                if (e.HopCount > 0)
                {
                    _hopAccumulator += e.HopCount;
                    _hopSamples++;
                    AvgHopCount.Set(_hopAccumulator / _hopSamples);
                }
            }

            RefreshDerived();
            // No Notify() here  -  UI is refreshed once per tick via the Ticked handler.
        };
        _engine.PacketExpired    += (_, _) =>
        {
            TotalPacketsExpired.Increment();
            RefreshDerived();
            // No Notify() here  -  UI is refreshed once per tick via the Ticked handler.
        };
        _engine.DeviceRegistered += (_, _) => { TotalDevicesAdded.Increment(); Notify(); };
        _engine.Ticked           += (_, e) =>
        {
            TotalTicks.Set(e.TickCount);

            // Skip the first dt sample: the engine timestamps _lastTickTime at
            // construction, so tick-1's dt includes all idle time before Start().
            // From tick 2 onward the delta reflects only the actual tick interval.
            if (_firstTickSeen)
            {
                _tickMsAccumulator += e.DtMs;
                _tickMsSamples++;
                AvgTickMs.Set(_tickMsAccumulator / _tickMsSamples);
            }
            else
            {
                _firstTickSeen = true;
            }

            // ActivePacketsCount is O(1); avoids allocating a list every tick.
            ActivePackets.Set(_engine.ActivePacketsCount);
            RefreshDerived();
            AppendSnapshot(e.TickCount);
            Notify();
        };
    }

    private sealed class ScopedStatisticsToken : IDisposable
    {
        private readonly SimulationStatistics? _previous;
        private bool _disposed;

        public ScopedStatisticsToken(SimulationStatistics? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            ScopedInstance.Value = _previous;
            _disposed = true;
        }
    }

    private void RefreshDerived()
    {
        var total = TotalPacketsDelivered.Value + TotalPacketsExpired.Value;
        DeliveryRate.Set(total > 0 ? TotalPacketsDelivered.Value / total * 100 : 0);
    }

    private void AppendSnapshot(long tick)
    {
        // Queue.Dequeue is O(1); replaces the old List.RemoveAt(0) which was O(N).
        if (_history.Count >= MaxHistoryLength)
            _history.Dequeue();

        _history.Enqueue(new TickSnapshot(
            Tick:               tick,
            ActivePackets:      ActivePackets.Value,
            TotalDelivered:     TotalPacketsDelivered.Value,
            TotalExpired:       TotalPacketsExpired.Value,
            DeliveryRate:       DeliveryRate.Value,
            TickMs:             AvgTickMs.Value,
            DuplicateDeliveries: DuplicateDeliveries.Value,
            AvgHopCount:        AvgHopCount.Value
        ));
    }

    /// <summary>
    /// Extracts the value for the given <paramref name="metric"/> from a
    /// <see cref="TickSnapshot"/>. Used by the chart to be series-agnostic.
    /// </summary>
    public static double GetSnapshotValue(TickSnapshot snapshot, StatMetric metric, IReadOnlyList<StatMetric> plottable)
    {
        for (int i = 0; i < plottable.Count; i++)
            if (ReferenceEquals(plottable[i], metric))
                return i switch
                {
                    0 => snapshot.TotalDelivered,
                    1 => snapshot.TotalExpired,
                    2 => snapshot.TickMs,
                    3 => snapshot.ActivePackets,
                    4 => snapshot.DeliveryRate,
                    5 => snapshot.DuplicateDeliveries,
                    6 => snapshot.AvgHopCount,
                    _ => 0
                };
        return 0;
    }

    /// <summary>Resets all metric counters and clears the history.</summary>
    public void Reset()
    {
        foreach (var m in Metrics) m.Reset();
        _history.Clear();
        _tickMsAccumulator = 0;
        _tickMsSamples     = 0;
        _firstTickSeen     = false;
        _deliveredOrigins.Clear();
        _hopAccumulator    = 0;
        _hopSamples        = 0;
        Notify();
    }

    private void Notify() => Updated?.Invoke();
}
