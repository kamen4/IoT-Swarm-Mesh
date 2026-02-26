using Engine.Core;

namespace Engine.Statistics;

/// <summary>
/// A singleton that listens to <see cref="SimulationEngine"/> events and
/// accumulates simulation metrics.
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
    /// <summary>Gets the singleton instance.</summary>
    public static SimulationStatistics Instance { get; } = new();

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
    public StatMetric DeliveryRate           { get; } = new("Delivery rate (%)",  "Delivered / (Delivered + Expired) ? 100",        isDecimal: true, isPlottable: true);

    /// <summary>All metrics in declaration order — drives the summary card grid.</summary>
    public IReadOnlyList<StatMetric> Metrics { get; }

    /// <summary>Metrics that can be selected as a chart series.</summary>
    public IReadOnlyList<StatMetric> PlottableMetrics { get; }

    /// <summary>
    /// Per-tick snapshots in chronological order, capped at
    /// <see cref="MaxHistoryLength"/>.
    /// </summary>
    public IReadOnlyList<TickSnapshot> History => _history;

    private readonly List<TickSnapshot> _history = [];
    private readonly SimulationEngine   _engine  = SimulationEngine.Instance;
    private double _tickMsAccumulator;
    private long   _tickMsSamples;

    private SimulationStatistics()
    {
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
        ];

        PlottableMetrics = Metrics.Where(m => m.IsPlottable).ToList();

        _engine.PacketRegistered += (_, _) => { TotalPacketsRegistered.Increment(); RefreshDerived(); Notify(); };
        _engine.PacketDelivered  += (_, _) => { TotalPacketsDelivered.Increment();  RefreshDerived(); Notify(); };
        _engine.PacketExpired    += (_, _) => { TotalPacketsExpired.Increment();    RefreshDerived(); Notify(); };
        _engine.DeviceRegistered += (_, _) => { TotalDevicesAdded.Increment();      Notify(); };
        _engine.Ticked           += (_, e) =>
        {
            TotalTicks.Set(e.TickCount);

            _tickMsAccumulator += e.DtMs;
            _tickMsSamples++;
            AvgTickMs.Set(_tickMsAccumulator / _tickMsSamples);

            ActivePackets.Set(_engine.ActivePackets.Count);
            RefreshDerived();
            AppendSnapshot(e.TickCount);
            Notify();
        };
    }

    private void RefreshDerived()
    {
        var total = TotalPacketsDelivered.Value + TotalPacketsExpired.Value;
        DeliveryRate.Set(total > 0 ? TotalPacketsDelivered.Value / total * 100 : 0);
    }

    private void AppendSnapshot(long tick)
    {
        if (_history.Count >= MaxHistoryLength)
            _history.RemoveAt(0);

        _history.Add(new TickSnapshot(
            Tick:           tick,
            ActivePackets:  ActivePackets.Value,
            TotalDelivered: TotalPacketsDelivered.Value,
            TotalExpired:   TotalPacketsExpired.Value,
            DeliveryRate:   DeliveryRate.Value,
            TickMs:         AvgTickMs.Value
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
        Notify();
    }

    private void Notify() => Updated?.Invoke();
}
