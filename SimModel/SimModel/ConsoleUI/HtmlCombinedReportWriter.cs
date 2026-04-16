using Engine.Benchmark;
using Engine.Devices;
using Engine.Packets;
using Engine.Routers;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace ConsoleUI;

/// <summary>
/// Writes one combined HTML report for all scenarios in a benchmark run.
/// </summary>
internal static class HtmlCombinedReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private sealed record ScenarioStats(
        int TotalNodes,
        int HubCount,
        int GeneratorCount,
        int EmitterCount,
        int RouterCount,
        int EventCount,
        int ToggleEvents,
        int RemoveEvents,
        int AddEvents);

    private sealed record TopologyNode(string Name, BenchmarkDeviceKind Kind, float X, float Y);

    private sealed record TopologyEdge(int A, int B);

    private sealed record RouterMetric(
        string RouterName,
        double TotalPacketsRegistered,
        double TotalPacketsDelivered,
        double TotalPacketsExpired,
        double DuplicateDeliveries,
        double DuplicateRate,
        double ExpireRate,
        double AvgHopCount,
        double DeliveryRate,
        double AvgTickMs);

    private sealed record HistoryPoint(
        long Tick,
        double ActivePackets,
        double TotalDelivered,
        double TotalExpired,
        double DeliveryRate,
        double DuplicateDeliveries,
        double AvgHopCount,
        double TickMs);

    private sealed record RouterHistory(
        string RouterName,
        IReadOnlyList<HistoryPoint> Points);

    private sealed record CombinedRun(
        string Id,
        string Label,
        string Name,
        string Description,
        string TopologyName,
        string SeedText,
      int TotalNodes,
        int VisibilityDistance,
        long DurationTicks,
        string Elapsed,
        string JsonPath,
        string BestRouter,
        double BestDeliveryRate,
        string MetricRowsHtml,
        string DerivedMetricRowsHtml,
        string ScenarioRowsHtml,
        string VectorRowsHtml,
        string EventRowsHtml,
        string TopologySvg,
        IReadOnlyList<RouterMetric> Metrics,
        IReadOnlyList<RouterHistory> History);

    private sealed class ReportDevice : Device
    {
        public ReportDevice(string name, Vector2 position)
        {
            Name = name;
            Position = position;
        }

        public override void Accept(Packet packet)
        {
        }
    }

    private sealed class ReportTopology : IMutableNetworkTopology
    {
        private readonly IReadOnlyList<Device> _devices;
        private readonly Dictionary<Guid, Device> _byId;
        private readonly HashSet<(Guid A, Guid B)> _connections = [];
        private readonly float _visibilityDistance;

        public ReportTopology(IReadOnlyList<Device> devices, int visibilityDistance)
        {
            _devices = devices;
            _byId = devices.ToDictionary(d => d.Id);
            _visibilityDistance = Math.Max(1, visibilityDistance);
        }

        public IEnumerable<Device> GetVisibleDevices(Device device)
        {
            foreach (var other in _devices)
            {
                if (other.Id == device.Id)
                    continue;

                if (AreVisible(device, other))
                    yield return other;
            }
        }

        public IEnumerable<Device> GetConnectedDevices(Device device)
        {
            foreach (var connection in _connections)
            {
                if (connection.A == device.Id && _byId.TryGetValue(connection.B, out var b))
                {
                    yield return b;
                    continue;
                }

                if (connection.B == device.Id && _byId.TryGetValue(connection.A, out var a))
                    yield return a;
            }
        }

        public bool AreVisible(Device a, Device b)
            => Vector2.Distance(a.Position, b.Position) <= _visibilityDistance;

        public bool AreConnected(Device a, Device b)
            => _connections.Contains(Normalize(a.Id, b.Id));

        public void Connect(Device a, Device b)
        {
            if (a.Id == b.Id)
                return;

            if (!AreVisible(a, b))
                return;

            _connections.Add(Normalize(a.Id, b.Id));
        }

        public void Disconnect(Device a, Device b)
            => _connections.Remove(Normalize(a.Id, b.Id));

        public void RemoveDevice(Device device)
        {
            var staleKeys = _connections
                .Where(c => c.A == device.Id || c.B == device.Id)
                .ToArray();

            foreach (var key in staleKeys)
                _connections.Remove(key);
        }

        public void ClearConnections()
            => _connections.Clear();

        private static (Guid A, Guid B) Normalize(Guid a, Guid b)
            => a.CompareTo(b) < 0 ? (a, b) : (b, a);
    }

    /// <summary>
    /// Generates one combined HTML report for all runs in the current command.
    /// </summary>
    /// <param name="outputDirectory">Report output directory.</param>
    /// <param name="runs">Scenario/session results of the current command.</param>
    /// <returns>Absolute path to the generated combined report.</returns>
    public static async Task<string> WriteAsync(
        string outputDirectory,
        IReadOnlyList<(BenchmarkScenario Scenario, BenchmarkSession Session, TimeSpan Duration, string JsonPath)> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);

        if (runs.Count == 0)
            throw new ArgumentException("At least one run is required.", nameof(runs));

        Directory.CreateDirectory(outputDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var filePath = Path.Combine(outputDirectory, $"benchmark_combined_{timestamp}_report.html");

        var preparedRuns = runs
            .Select((entry, i) => PrepareRun(entry, i + 1))
            .ToArray();

        var summaryRows = BuildSummaryRows(preparedRuns);
        var runsJson = JsonSerializer.Serialize(preparedRuns, JsonOptions);

        var generatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

        var html = $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Combined Benchmark Report</title>
  <style>
    :root {
      --bg: #f4f6fa;
      --panel: #ffffff;
      --ink: #0f172a;
      --muted: #475569;
      --line: #d7dee8;
      --accent: #0f766e;
      --accent-2: #2563eb;
      --radius: 14px;
    }

    * {
      box-sizing: border-box;
    }

    html,
    body {
      overflow-x: hidden;
    }

    body {
      margin: 0;
      padding: 20px;
      background: radial-gradient(circle at top right, #d9f7ef 0%, var(--bg) 48%), var(--bg);
      color: var(--ink);
      font-family: "Segoe UI", Tahoma, sans-serif;
    }

    .wrap {
      max-width: 1240px;
      margin: 0 auto;
      display: grid;
      gap: 16px;
    }

    .hero {
      border-radius: var(--radius);
      background: linear-gradient(126deg, #0f766e 0%, #0b5a64 58%, #123c4b 100%);
      color: #ecfeff;
      padding: 22px;
      box-shadow: 0 10px 24px rgba(15, 23, 42, 0.16);
    }

    .hero h1 {
      margin: 0;
      font-size: 1.6rem;
    }

    .hero p {
      margin: 8px 0 0;
      color: #d3fcf4;
      line-height: 1.45;
    }

    .meta-row {
      margin-top: 12px;
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      font-size: 0.88rem;
      color: #b8f8ec;
    }

    .panel {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: var(--radius);
      padding: 14px;
      min-width: 0;
      box-shadow: 0 8px 20px rgba(15, 23, 42, 0.05);
    }

    .panel h2 {
      margin: 0 0 10px;
      font-size: 1.02rem;
    }

    .panel h3 {
      margin: 0 0 8px;
      font-size: 0.94rem;
      color: var(--muted);
    }

    .table-scroll {
      overflow-x: auto;
      max-width: 100%;
      -webkit-overflow-scrolling: touch;
    }

    table {
      width: max-content;
      min-width: 100%;
      border-collapse: collapse;
      font-size: 0.88rem;
    }

    table.compact {
      min-width: 380px;
    }

    thead th {
      text-align: left;
      font-size: 0.76rem;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--muted);
      border-bottom: 1px solid var(--line);
      padding: 9px 8px;
      white-space: nowrap;
    }

    tbody td {
      border-bottom: 1px solid #e9eef5;
      padding: 8px;
      vertical-align: top;
      white-space: normal;
      word-break: break-word;
      overflow-wrap: anywhere;
    }

    tbody tr:last-child td {
      border-bottom: none;
    }

    td.wrap-cell,
    th.wrap-cell {
      white-space: normal;
      word-break: break-word;
      overflow-wrap: anywhere;
      min-width: 200px;
    }

    .mono {
      font-family: "Cascadia Code", "Consolas", monospace;
      font-size: 0.82rem;
      word-break: break-word;
      overflow-wrap: anywhere;
    }

    .controls {
      display: grid;
      grid-template-columns: minmax(300px, 1fr) 2fr;
      gap: 14px;
      align-items: end;
    }

    .controls label {
      display: grid;
      gap: 5px;
      font-size: 0.84rem;
      color: var(--muted);
    }

    .controls select {
      height: 36px;
      width: 100%;
      border-radius: 8px;
      border: 1px solid #c6d0de;
      padding: 0 10px;
      background: #f9fbff;
      color: var(--ink);
    }

    .run-meta-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
      gap: 8px;
    }

    .meta-chip {
      border: 1px solid #d8e0eb;
      border-radius: 8px;
      padding: 8px;
      background: #f8fbff;
      display: grid;
      gap: 4px;
    }

    .meta-chip b {
      font-size: 0.74rem;
      color: var(--muted);
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }

    .meta-chip span {
      font-size: 0.87rem;
      color: var(--ink);
      word-break: break-word;
    }

    .split {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 14px;
    }

    .topology-wrap {
      border: 1px solid #dbe4ef;
      border-radius: 10px;
      overflow: auto;
      background: #f8fafd;
    }

    .topology-svg {
      display: block;
      width: 100%;
      min-width: 760px;
      height: auto;
    }

    .chart-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 14px;
    }

    .chart-box {
      position: relative;
      height: 320px;
    }

    canvas {
      width: 100% !important;
      height: 100% !important;
    }

    .note {
      color: var(--muted);
      font-size: 0.82rem;
      word-break: break-word;
      overflow-wrap: anywhere;
    }

    @media (max-width: 900px) {
      body {
        padding: 10px;
      }

      .controls {
        grid-template-columns: 1fr;
      }

      .chart-box {
        height: 260px;
      }
    }
  </style>
</head>
<body>
  <div class="wrap">
    <section class="hero">
      <h1>Combined Benchmark Report</h1>
      <p>Single report for all scenarios in this benchmark command, including seed and topology sweeps.</p>
      <div class="meta-row">
        <span>Generated: {{generatedAt}}</span>
        <span>Runs: {{preparedRuns.Length}}</span>
      </div>
    </section>

    <section class="panel">
      <h2>Overall Router Aggregate</h2>
      <p class="note">Mean / median / best / worst statistics across all runs for each router.</p>
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th>Router</th>
              <th>Runs</th>
              <th>Delivery % (mean / median / best / worst)</th>
              <th>Avg tick ms (mean / median / best / worst)</th>
              <th>Dup rate % (mean / median / best / worst)</th>
              <th>Best case</th>
              <th>Worst case</th>
            </tr>
          </thead>
          <tbody id="overallRouterRows"></tbody>
        </table>
      </div>
    </section>

    <section class="panel">
      <h2>Topology Aggregate</h2>
      <p class="note">Group summary by topology batch (node count + visibility distance).</p>
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th>Topology batch</th>
              <th>Runs</th>
              <th>Best router by median delivery</th>
              <th>Median delivery %</th>
              <th>Worst router by median delivery</th>
              <th>Median delivery %</th>
            </tr>
          </thead>
          <tbody id="overallTopologyRows"></tbody>
        </table>
      </div>
    </section>

    <section class="panel">
      <h2>Run Summary</h2>
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th>#</th>
              <th class="wrap-cell">Scenario</th>
              <th>Nodes / visibility</th>
              <th>Elapsed</th>
              <th>Best router</th>
              <th>Best delivery %</th>
              <th class="wrap-cell">Session JSON</th>
            </tr>
          </thead>
          <tbody>
{{summaryRows}}
          </tbody>
        </table>
      </div>
    </section>

    <section class="panel controls">
      <label>
        Choose run
        <select id="runSelect"></select>
      </label>
      <div class="run-meta-grid">
        <div class="meta-chip"><b>Scenario</b><span id="metaName"></span></div>
        <div class="meta-chip"><b>Topology</b><span id="metaTopology"></span></div>
        <div class="meta-chip"><b>Nodes</b><span id="metaNodes"></span></div>
        <div class="meta-chip"><b>Seed</b><span id="metaSeed"></span></div>
        <div class="meta-chip"><b>Elapsed</b><span id="metaElapsed"></span></div>
        <div class="meta-chip"><b>Visibility</b><span id="metaVisibility"></span></div>
        <div class="meta-chip"><b>Duration ticks</b><span id="metaTicks"></span></div>
      </div>
    </section>

    <section class="panel">
      <h2 id="runTitle"></h2>
      <p id="runDescription" class="note"></p>
      <p class="note">Session JSON: <span id="runJson" class="mono"></span></p>
    </section>

    <section class="split">
      <div class="panel">
        <h2>Final Router Metrics</h2>
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Router</th>
                <th>Delivery %</th>
                <th>Delivered</th>
                <th>Expired</th>
                <th>Duplicates</th>
                <th>Avg hops</th>
                <th>Avg tick ms</th>
              </tr>
            </thead>
            <tbody id="metricRows"></tbody>
          </table>
        </div>
      </div>

      <div class="panel">
        <h2>Derived Router Metrics</h2>
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Router</th>
                <th>Unique delivered</th>
                <th>Dup rate %</th>
                <th>Expire rate %</th>
                <th>Registered / delivered</th>
              </tr>
            </thead>
            <tbody id="derivedRows"></tbody>
          </table>
        </div>
      </div>
    </section>

    <section class="split">
      <div class="panel">
        <h2>Scenario Composition</h2>
        <div class="table-scroll">
          <table class="compact">
            <tbody id="scenarioRows"></tbody>
          </table>
        </div>
      </div>

      <div class="panel">
        <h2>Swarm Vector</h2>
        <div class="table-scroll">
          <table class="compact">
            <tbody id="vectorRows"></tbody>
          </table>
        </div>
      </div>
    </section>

    <section class="panel">
      <h2>Initial Topology Snapshot</h2>
      <div id="topologySvg" class="topology-wrap"></div>
    </section>

    <section class="panel">
      <h2>Event Timeline Preview</h2>
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th>Tick</th>
              <th class="wrap-cell">Event</th>
            </tr>
          </thead>
          <tbody id="eventRows"></tbody>
        </table>
      </div>
    </section>

    <section class="chart-grid">
      <div class="panel">
        <h2>Delivery Rate by Router</h2>
        <div class="chart-box"><canvas id="deliveryBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Registered Packets by Router</h2>
        <div class="chart-box"><canvas id="registeredBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Delivered Packets by Router</h2>
        <div class="chart-box"><canvas id="deliveredBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Expired Packets by Router</h2>
        <div class="chart-box"><canvas id="expiredBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Duplicate Deliveries by Router</h2>
        <div class="chart-box"><canvas id="duplicatesBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Duplicate Rate by Router</h2>
        <div class="chart-box"><canvas id="duplicateRateBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Expire Rate by Router</h2>
        <div class="chart-box"><canvas id="expireRateBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Average Hop Count by Router</h2>
        <div class="chart-box"><canvas id="hopBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Average Tick Cost by Router</h2>
        <div class="chart-box"><canvas id="tickBar"></canvas></div>
      </div>

      <div class="panel">
        <h2>Delivery Rate over Time</h2>
        <div class="chart-box"><canvas id="deliveryTimeline"></canvas></div>
      </div>

      <div class="panel">
        <h2>Active Packets over Time</h2>
        <div class="chart-box"><canvas id="activeTimeline"></canvas></div>
      </div>

      <div class="panel">
        <h2>Delivered Packets over Time</h2>
        <div class="chart-box"><canvas id="deliveredTimeline"></canvas></div>
      </div>

      <div class="panel">
        <h2>Expired Packets over Time</h2>
        <div class="chart-box"><canvas id="expiredTimeline"></canvas></div>
      </div>

      <div class="panel">
        <h2>Duplicate Deliveries over Time</h2>
        <div class="chart-box"><canvas id="duplicateTimeline"></canvas></div>
      </div>

      <div class="panel">
        <h2>Average Hop Count over Time</h2>
        <div class="chart-box"><canvas id="hopTimeline"></canvas></div>
      </div>

      <div class="panel">
        <h2>Average Tick Cost over Time</h2>
        <div class="chart-box"><canvas id="tickTimeline"></canvas></div>
      </div>
    </section>

    <section class="panel note">
      This combined report uses a single responsive layout and one chart set bound to the selected run to keep large seed/topology sweeps readable.
    </section>
  </div>

  <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
  <script>
    const runs = {{runsJson}};

    const colors = [
      "#0f766e",
      "#2563eb",
      "#f59e0b",
      "#dc2626",
      "#7c3aed",
      "#0ea5e9",
      "#64748b",
      "#16a34a"
    ];

    const chartIds = [
      "deliveryBar",
      "registeredBar",
      "deliveredBar",
      "expiredBar",
      "duplicatesBar",
      "duplicateRateBar",
      "expireRateBar",
      "hopBar",
      "tickBar",
      "deliveryTimeline",
      "activeTimeline",
      "deliveredTimeline",
      "expiredTimeline",
      "duplicateTimeline",
      "hopTimeline",
      "tickTimeline"
    ];

    const charts = Object.fromEntries(chartIds.map(id => [id, null]));

    function colorFor(index) {
      return colors[index % colors.length];
    }

    function selectEl(id) {
      return document.getElementById(id);
    }

    function setText(id, value) {
      const el = selectEl(id);
      if (!el) {
        return;
      }

      el.textContent = value;
    }

    function setHtml(id, value) {
      const el = selectEl(id);
      if (!el) {
        return;
      }

      el.innerHTML = value;
    }

    function escapeHtml(value) {
      return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
    }

    function formatNumber(value, digits = 3) {
      const number = Number(value);
      if (!Number.isFinite(number)) {
        return "0";
      }

      return number.toFixed(digits).replace(/\.0+$/, "").replace(/(\.\d*[1-9])0+$/, "$1");
    }

    function mean(values) {
      if (!values.length) {
        return 0;
      }

      return values.reduce((sum, x) => sum + x, 0) / values.length;
    }

    function median(values) {
      if (!values.length) {
        return 0;
      }

      const sorted = [...values].sort((a, b) => a - b);
      const middle = Math.floor(sorted.length / 2);
      if (sorted.length % 2 === 0) {
        return (sorted[middle - 1] + sorted[middle]) / 2;
      }

      return sorted[middle];
    }

    function selectExtrema(entries, selector, higherIsBetter) {
      if (!entries.length) {
        return { best: null, worst: null };
      }

      let best = entries[0];
      let worst = entries[0];

      for (let i = 1; i < entries.length; i++) {
        const current = entries[i];
        const currentValue = selector(current);
        const bestValue = selector(best);
        const worstValue = selector(worst);

        if (higherIsBetter ? currentValue > bestValue : currentValue < bestValue) {
          best = current;
        }

        if (higherIsBetter ? currentValue < worstValue : currentValue > worstValue) {
          worst = current;
        }
      }

      return { best, worst };
    }

    function renderOverallRouterAggregate() {
      const byRouter = new Map();

      runs.forEach(run => {
        run.metrics.forEach(metric => {
          const list = byRouter.get(metric.routerName) ?? [];
          list.push({ run, metric });
          byRouter.set(metric.routerName, list);
        });
      });

      const rows = [...byRouter.entries()]
        .map(([routerName, entries]) => {
          const delivery = entries.map(x => x.metric.deliveryRate);
          const tickMs = entries.map(x => x.metric.avgTickMs);
          const dupRate = entries.map(x => x.metric.duplicateRate);

          const deliveryExtrema = selectExtrema(entries, x => x.metric.deliveryRate, true);

          const deliveryStats =
            `${formatNumber(mean(delivery), 2)} / ${formatNumber(median(delivery), 2)} / ` +
            `${formatNumber(deliveryExtrema.best?.metric.deliveryRate ?? 0, 2)} / ${formatNumber(deliveryExtrema.worst?.metric.deliveryRate ?? 0, 2)}`;

          const tickStats =
            `${formatNumber(mean(tickMs), 3)} / ${formatNumber(median(tickMs), 3)} / ` +
            `${formatNumber(Math.min(...tickMs), 3)} / ${formatNumber(Math.max(...tickMs), 3)}`;

          const dupStats =
            `${formatNumber(mean(dupRate), 2)} / ${formatNumber(median(dupRate), 2)} / ` +
            `${formatNumber(Math.min(...dupRate), 2)} / ${formatNumber(Math.max(...dupRate), 2)}`;

          return {
            routerName,
            medianDelivery: median(delivery),
            html:
              `<tr>` +
              `<td>${escapeHtml(routerName)}</td>` +
              `<td class="mono">${entries.length}</td>` +
              `<td class="mono">${deliveryStats}</td>` +
              `<td class="mono">${tickStats}</td>` +
              `<td class="mono">${dupStats}</td>` +
              `<td class="wrap-cell">${escapeHtml(deliveryExtrema.best?.run.label ?? "n/a")}</td>` +
              `<td class="wrap-cell">${escapeHtml(deliveryExtrema.worst?.run.label ?? "n/a")}</td>` +
              `</tr>`
          };
        })
        .sort((a, b) => b.medianDelivery - a.medianDelivery)
        .map(x => x.html);

      setHtml("overallRouterRows", rows.length
        ? rows.join("\n")
        : `<tr><td colspan="7" class="mono">No aggregate data.</td></tr>`);
    }

    function renderOverallTopologyAggregate() {
      const byTopology = new Map();

      runs.forEach(run => {
        const key = `${run.totalNodes} nodes / vis ${run.visibilityDistance}`;
        const list = byTopology.get(key) ?? [];
        list.push(run);
        byTopology.set(key, list);
      });

      const rows = [...byTopology.entries()]
        .map(([topologyKey, groupedRuns]) => {
          const routerMap = new Map();

          groupedRuns.forEach(run => {
            run.metrics.forEach(metric => {
              const list = routerMap.get(metric.routerName) ?? [];
              list.push(metric.deliveryRate);
              routerMap.set(metric.routerName, list);
            });
          });

          const ranked = [...routerMap.entries()]
            .map(([routerName, values]) => ({
              routerName,
              medianDelivery: median(values)
            }))
            .sort((a, b) => b.medianDelivery - a.medianDelivery);

          const best = ranked[0];
          const worst = ranked[ranked.length - 1];

          return {
            groupedRuns,
            topologyKey,
            html:
              `<tr>` +
              `<td>${escapeHtml(topologyKey)}</td>` +
              `<td class="mono">${groupedRuns.length}</td>` +
              `<td>${escapeHtml(best?.routerName ?? "n/a")}</td>` +
              `<td class="mono">${formatNumber(best?.medianDelivery ?? 0, 2)}</td>` +
              `<td>${escapeHtml(worst?.routerName ?? "n/a")}</td>` +
              `<td class="mono">${formatNumber(worst?.medianDelivery ?? 0, 2)}</td>` +
              `</tr>`
          };
        })
        .sort((a, b) => {
          const aNodes = Number(a.groupedRuns[0]?.totalNodes ?? 0);
          const bNodes = Number(b.groupedRuns[0]?.totalNodes ?? 0);
          if (aNodes !== bNodes) {
            return aNodes - bNodes;
          }

          return Number(a.groupedRuns[0]?.visibilityDistance ?? 0) - Number(b.groupedRuns[0]?.visibilityDistance ?? 0);
        })
        .map(x => x.html);

      setHtml("overallTopologyRows", rows.length
        ? rows.join("\n")
        : `<tr><td colspan="6" class="mono">No topology aggregate data.</td></tr>`);
    }

    function renderOverallAggregates() {
      renderOverallRouterAggregate();
      renderOverallTopologyAggregate();
    }

    function destroyChart(key) {
      if (!charts[key]) {
        return;
      }

      charts[key].destroy();
      charts[key] = null;
    }

    function destroyAllCharts() {
      Object.keys(charts).forEach(destroyChart);
    }

    function buildYScale(title, maxValue) {
      const scale = {
        beginAtZero: true,
        title: {
          display: true,
          text: title
        }
      };

      if (typeof maxValue === "number") {
        scale.max = maxValue;
      }

      return scale;
    }

    function createBarChart(canvasId, labels, values, label, maxValue) {
      return new Chart(selectEl(canvasId), {
        type: "bar",
        data: {
          labels,
          datasets: [{
            label,
            data: values,
            backgroundColor: labels.map((_, i) => colorFor(i))
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          scales: {
            y: buildYScale(label, maxValue)
          }
        }
      });
    }

    function lineSeries(history, index, mapper, label) {
      return {
        label,
        data: history.points.map(p => ({ x: p.tick, y: mapper(p) })),
        borderColor: colorFor(index),
        backgroundColor: colorFor(index),
        borderWidth: 2,
        pointRadius: 0,
        tension: 0.15
      };
    }

    function createTimelineChart(canvasId, runHistory, mapper, yLabel, maxValue) {
      return new Chart(selectEl(canvasId), {
        type: "line",
        data: {
          datasets: runHistory.map((series, i) =>
            lineSeries(series, i, mapper, series.routerName)
          )
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          parsing: false,
          scales: {
            x: { type: "linear", title: { display: true, text: "Tick" } },
            y: buildYScale(yLabel, maxValue)
          }
        }
      });
    }

    function renderCharts(run) {
      destroyAllCharts();

      const labels = run.metrics.map(m => m.routerName);

      charts.deliveryBar = createBarChart(
        "deliveryBar",
        labels,
        run.metrics.map(m => m.deliveryRate),
        "Delivery rate (%)",
        100);

      charts.registeredBar = createBarChart(
        "registeredBar",
        labels,
        run.metrics.map(m => m.totalPacketsRegistered),
        "Registered packets");

      charts.deliveredBar = createBarChart(
        "deliveredBar",
        labels,
        run.metrics.map(m => m.totalPacketsDelivered),
        "Delivered packets");

      charts.expiredBar = createBarChart(
        "expiredBar",
        labels,
        run.metrics.map(m => m.totalPacketsExpired),
        "Expired packets");

      charts.duplicatesBar = createBarChart(
        "duplicatesBar",
        labels,
        run.metrics.map(m => m.duplicateDeliveries),
        "Duplicate deliveries");

      charts.duplicateRateBar = createBarChart(
        "duplicateRateBar",
        labels,
        run.metrics.map(m => m.duplicateRate),
        "Duplicate rate (%)");

      charts.expireRateBar = createBarChart(
        "expireRateBar",
        labels,
        run.metrics.map(m => m.expireRate),
        "Expire rate (%)");

      charts.hopBar = createBarChart(
        "hopBar",
        labels,
        run.metrics.map(m => m.avgHopCount),
        "Average hop count");

      charts.tickBar = createBarChart(
        "tickBar",
        labels,
        run.metrics.map(m => m.avgTickMs),
        "Average tick ms");

      charts.deliveryTimeline = createTimelineChart(
        "deliveryTimeline",
        run.history,
        p => p.deliveryRate,
        "Delivery %",
        100);

      charts.activeTimeline = createTimelineChart(
        "activeTimeline",
        run.history,
        p => p.activePackets,
        "Active packets");

      charts.deliveredTimeline = createTimelineChart(
        "deliveredTimeline",
        run.history,
        p => p.totalDelivered,
        "Delivered packets");

      charts.expiredTimeline = createTimelineChart(
        "expiredTimeline",
        run.history,
        p => p.totalExpired,
        "Expired packets");

      charts.duplicateTimeline = createTimelineChart(
        "duplicateTimeline",
        run.history,
        p => p.duplicateDeliveries,
        "Duplicate deliveries");

      charts.hopTimeline = createTimelineChart(
        "hopTimeline",
        run.history,
        p => p.avgHopCount,
        "Average hop count");

      charts.tickTimeline = createTimelineChart(
        "tickTimeline",
        run.history,
        p => p.tickMs,
        "Tick ms");
    }

    function renderRun(index) {
      const run = runs[index];
      if (!run) {
        return;
      }

      setText("metaName", run.name);
      setText("metaTopology", run.topologyName);
      setText("metaNodes", String(run.totalNodes));
      setText("metaSeed", run.seedText);
      setText("metaElapsed", run.elapsed);
      setText("metaVisibility", run.visibilityDistance + " units");
      setText("metaTicks", String(run.durationTicks));

      setText("runTitle", run.label);
      setText("runDescription", run.description);
      setText("runJson", run.jsonPath);

      setHtml("metricRows", run.metricRowsHtml);
      setHtml("derivedRows", run.derivedMetricRowsHtml);
      setHtml("scenarioRows", run.scenarioRowsHtml);
      setHtml("vectorRows", run.vectorRowsHtml);
      setHtml("eventRows", run.eventRowsHtml);
      setHtml("topologySvg", run.topologySvg);

      renderCharts(run);
    }

    function initSelect() {
      const select = selectEl("runSelect");
      runs.forEach((run, i) => {
        const option = document.createElement("option");
        option.value = String(i);
        option.textContent = run.label;
        select.appendChild(option);
      });

      select.addEventListener("change", event => {
        const nextIndex = Number(event.target.value || "0");
        renderRun(nextIndex);
      });
    }

    renderOverallAggregates();
    initSelect();
    renderRun(0);
  </script>
</body>
</html>
""";

        await File.WriteAllTextAsync(filePath, html);
        return filePath;
    }

    private static CombinedRun PrepareRun(
        (BenchmarkScenario Scenario, BenchmarkSession Session, TimeSpan Duration, string JsonPath) entry,
        int index)
    {
        var scenario = entry.Scenario;
        var session = entry.Session;

        var stats = BuildScenarioStats(scenario.Config);
        var topology = BuildTopologySnapshot(scenario);

        var metrics = session.Results
            .Select(r => new RouterMetric(
                RouterName: r.RouterName,
                TotalPacketsRegistered: r.TotalPacketsRegistered,
                TotalPacketsDelivered: r.TotalPacketsDelivered,
                TotalPacketsExpired: r.TotalPacketsExpired,
                DuplicateDeliveries: r.DuplicateDeliveries,
                DuplicateRate: r.TotalPacketsDelivered <= 0
                    ? 0
                    : (r.DuplicateDeliveries / r.TotalPacketsDelivered) * 100.0,
                ExpireRate: r.TotalPacketsRegistered <= 0
                    ? 0
                    : (r.TotalPacketsExpired / r.TotalPacketsRegistered) * 100.0,
                AvgHopCount: r.AvgHopCount,
                DeliveryRate: r.DeliveryRate,
                AvgTickMs: r.AvgTickMs))
            .ToArray();

        var history = session.Results
            .Select(r => new RouterHistory(
                RouterName: r.RouterName,
                Points: r.History.Select(h => new HistoryPoint(
                    Tick: h.Tick,
                    ActivePackets: h.ActivePackets,
                    TotalDelivered: h.TotalDelivered,
                    TotalExpired: h.TotalExpired,
                    DeliveryRate: h.DeliveryRate,
                    DuplicateDeliveries: h.DuplicateDeliveries,
                    AvgHopCount: h.AvgHopCount,
                    TickMs: h.TickMs)).ToArray()))
            .ToArray();

        var best = metrics
            .OrderByDescending(m => m.DeliveryRate)
            .ThenBy(m => m.DuplicateDeliveries)
            .FirstOrDefault();

        var seedText = scenario.SeedTemplate?.Seed > 0
            ? scenario.SeedTemplate.Seed.ToString(CultureInfo.InvariantCulture)
            : "n/a";

        var topologyText = scenario.TopologyBuilder.Name;
        var label = $"{index}. {scenario.Name} [topology: {topologyText}, seed: {seedText}]";

        return new CombinedRun(
            Id: $"run-{index}",
            Label: label,
            Name: scenario.Name,
            Description: scenario.Description,
            TopologyName: topologyText,
            SeedText: seedText,
          TotalNodes: stats.TotalNodes,
            VisibilityDistance: scenario.Config.VisibilityDistance,
            DurationTicks: scenario.Config.DurationTicks,
            Elapsed: entry.Duration.ToString("mm\\:ss", CultureInfo.InvariantCulture),
            JsonPath: entry.JsonPath,
            BestRouter: best?.RouterName ?? "n/a",
            BestDeliveryRate: best?.DeliveryRate ?? 0,
            MetricRowsHtml: BuildMetricRows(metrics),
            DerivedMetricRowsHtml: BuildDerivedMetricRows(metrics),
            ScenarioRowsHtml: BuildScenarioRows(stats),
            VectorRowsHtml: BuildVectorRows(scenario.Vector),
            EventRowsHtml: BuildEventRows(scenario.Config.Events, maxRows: 18),
            TopologySvg: BuildTopologySvg(topology.Nodes, topology.Edges),
            Metrics: metrics,
            History: history);
    }

    private static string BuildSummaryRows(IReadOnlyList<CombinedRun> runs)
    {
        var rows = runs.Select((run, i) =>
            "            <tr>" +
            $"<td class=\"mono\">{i + 1}</td>" +
            $"<td class=\"wrap-cell\">{WebUtility.HtmlEncode(run.Name)}</td>" +
        $"<td class=\"mono\">{run.TotalNodes} / {run.VisibilityDistance}</td>" +
            $"<td class=\"mono\">{WebUtility.HtmlEncode(run.Elapsed)}</td>" +
            $"<td>{WebUtility.HtmlEncode(run.BestRouter)}</td>" +
            $"<td class=\"mono\">{FormatNumber(run.BestDeliveryRate)}%</td>" +
            $"<td class=\"wrap-cell mono\">{WebUtility.HtmlEncode(run.JsonPath)}</td>" +
            "</tr>");

        return string.Join(Environment.NewLine, rows);
    }

    private static ScenarioStats BuildScenarioStats(BenchmarkConfig config)
    {
        var hubs = config.Devices.Count(d => d.Kind == BenchmarkDeviceKind.Hub);
        var generators = config.Devices.Count(d => d.Kind == BenchmarkDeviceKind.Generator);
        var emitters = config.Devices.Count(d => d.Kind == BenchmarkDeviceKind.Emitter);

        var toggleEvents = config.Events.Count(e => e.Event is ToggleBenchmarkEvent);
        var removeEvents = config.Events.Count(e => e.Event is RemoveDeviceBenchmarkEvent);
        var addEvents = config.Events.Count(e => e.Event is AddDeviceBenchmarkEvent);

        return new ScenarioStats(
            TotalNodes: config.Devices.Count,
            HubCount: hubs,
            GeneratorCount: generators,
            EmitterCount: emitters,
            RouterCount: config.RouterNames.Count,
            EventCount: config.Events.Count,
            ToggleEvents: toggleEvents,
            RemoveEvents: removeEvents,
            AddEvents: addEvents);
    }

    private static (IReadOnlyList<TopologyNode> Nodes, IReadOnlyList<TopologyEdge> Edges) BuildTopologySnapshot(
        BenchmarkScenario scenario)
    {
        var nodes = scenario.Config.Devices
            .Select(d => new TopologyNode(d.Name, d.Kind, d.X, d.Y))
            .ToArray();

        if (nodes.Length == 0)
            return (nodes, []);

        var reportDevices = nodes
            .Select(n => (Device)new ReportDevice(n.Name, new Vector2(n.X, n.Y)))
            .ToArray();

        var topology = new ReportTopology(reportDevices, scenario.Config.VisibilityDistance);

        try
        {
            scenario.TopologyBuilder.Build(reportDevices, topology);
        }
        catch
        {
            new FullMeshNetworkBuilder().Build(reportDevices, topology);
        }

        var edges = new List<TopologyEdge>();
        for (var i = 0; i < reportDevices.Length; i++)
        {
            for (var j = i + 1; j < reportDevices.Length; j++)
            {
                if (!topology.AreConnected(reportDevices[i], reportDevices[j]))
                    continue;

                edges.Add(new TopologyEdge(i, j));
            }
        }

        return (nodes, edges);
    }

    private static string BuildMetricRows(IEnumerable<RouterMetric> results)
    {
        var rows = results.Select(result =>
            "            <tr>" + Environment.NewLine +
            $"              <td>{WebUtility.HtmlEncode(result.RouterName)}</td>" + Environment.NewLine +
            $"              <td class=\"mono\">{FormatNumber(result.DeliveryRate)}%</td>" + Environment.NewLine +
            $"              <td class=\"mono\">{FormatNumber(result.TotalPacketsDelivered)}</td>" + Environment.NewLine +
            $"              <td class=\"mono\">{FormatNumber(result.TotalPacketsExpired)}</td>" + Environment.NewLine +
            $"              <td class=\"mono\">{FormatNumber(result.DuplicateDeliveries)}</td>" + Environment.NewLine +
            $"              <td class=\"mono\">{FormatNumber(result.AvgHopCount)}</td>" + Environment.NewLine +
            $"              <td class=\"mono\">{FormatNumber(result.AvgTickMs)}</td>" + Environment.NewLine +
            "            </tr>");

        return string.Join(Environment.NewLine, rows);
    }

    private static string BuildDerivedMetricRows(IEnumerable<RouterMetric> results)
    {
        var rows = results.Select(result =>
        {
            var uniqueDelivered = Math.Max(0.0, result.TotalPacketsDelivered - result.DuplicateDeliveries);
            var registerPerDelivered = result.TotalPacketsDelivered <= 0
                ? 0
                : result.TotalPacketsRegistered / result.TotalPacketsDelivered;

            return "            <tr>" + Environment.NewLine +
                   $"              <td>{WebUtility.HtmlEncode(result.RouterName)}</td>" + Environment.NewLine +
                   $"              <td class=\"mono\">{FormatNumber(uniqueDelivered)}</td>" + Environment.NewLine +
                   $"              <td class=\"mono\">{FormatNumber(result.DuplicateRate)}%</td>" + Environment.NewLine +
                   $"              <td class=\"mono\">{FormatNumber(result.ExpireRate)}%</td>" + Environment.NewLine +
                   $"              <td class=\"mono\">{FormatNumber(registerPerDelivered)}</td>" + Environment.NewLine +
                   "            </tr>";
        });

        return string.Join(Environment.NewLine, rows);
    }

    private static string BuildVectorRows(SwarmProtocolVector vector)
    {
        var rows = new (string Name, double Value)[]
        {
            ("qForward", vector.QForward),
            ("rootSourceCharge", vector.RootSourceCharge),
            ("penaltyLambda", vector.PenaltyLambda),
            ("switchHysteresis", vector.SwitchHysteresis),
            ("switchHysteresisRatio", vector.SwitchHysteresisRatio),
            ("parentDeadTicks", vector.ParentDeadTicks),
            ("chargeDropPerHop", vector.ChargeDropPerHop),
            ("chargeSpreadFactor", vector.ChargeSpreadFactor),
            ("decayIntervalSteps", vector.DecayIntervalSteps),
            ("decayPercent", vector.DecayPercent),
            ("linkMemory", vector.LinkMemory),
            ("linkLearningRate", vector.LinkLearningRate),
            ("linkBonusMax", vector.LinkBonusMax),
        };

        return string.Join(
            Environment.NewLine,
            rows.Select(r =>
                $"            <tr><td>{WebUtility.HtmlEncode(r.Name)}</td><td class=\"mono\">{FormatNumber(r.Value)}</td></tr>"));
    }

    private static string BuildScenarioRows(ScenarioStats stats)
    {
        var rows = new (string Name, string Value)[]
        {
            ("totalNodes", stats.TotalNodes.ToString(CultureInfo.InvariantCulture)),
            ("hubCount", stats.HubCount.ToString(CultureInfo.InvariantCulture)),
            ("generatorCount", stats.GeneratorCount.ToString(CultureInfo.InvariantCulture)),
            ("emitterCount", stats.EmitterCount.ToString(CultureInfo.InvariantCulture)),
            ("routerCount", stats.RouterCount.ToString(CultureInfo.InvariantCulture)),
            ("eventCount", stats.EventCount.ToString(CultureInfo.InvariantCulture)),
            ("toggleEvents", stats.ToggleEvents.ToString(CultureInfo.InvariantCulture)),
            ("removeEvents", stats.RemoveEvents.ToString(CultureInfo.InvariantCulture)),
            ("addEvents", stats.AddEvents.ToString(CultureInfo.InvariantCulture)),
        };

        return string.Join(
            Environment.NewLine,
            rows.Select(r =>
                $"            <tr><td>{WebUtility.HtmlEncode(r.Name)}</td><td class=\"mono\">{WebUtility.HtmlEncode(r.Value)}</td></tr>"));
    }

    private static string BuildEventRows(IReadOnlyList<BenchmarkEventEntry> events, int maxRows)
    {
        if (events.Count == 0)
            return "            <tr><td colspan=\"2\" class=\"mono\">No scheduled events.</td></tr>";

        var rows = events
            .OrderBy(e => e.AtTick)
            .Take(Math.Max(1, maxRows))
            .Select(entry =>
                "            <tr>" +
                $"<td class=\"mono\">{entry.AtTick}</td>" +
                $"<td class=\"wrap-cell\">{WebUtility.HtmlEncode(DescribeEvent(entry.Event))}</td>" +
                "</tr>")
            .ToList();

        if (events.Count > maxRows)
            rows.Add($"            <tr><td colspan=\"2\" class=\"mono\">... and {events.Count - maxRows} more events</td></tr>");

        return string.Join(Environment.NewLine, rows);
    }

    private static string DescribeEvent(BenchmarkEvent evt)
    {
        return evt switch
        {
            ToggleBenchmarkEvent t => $"toggle emitter '{t.DeviceName}'",
            RemoveDeviceBenchmarkEvent r => $"remove device '{r.DeviceName}'",
            AddDeviceBenchmarkEvent a => $"add {a.Device.Kind} '{a.Device.Name}'",
            _ => evt.GetType().Name,
        };
    }

    private static string BuildTopologySvg(
        IReadOnlyList<TopologyNode> nodes,
        IReadOnlyList<TopologyEdge> edges)
    {
        if (nodes.Count == 0)
            return "<div class='note' style='padding:12px'>No topology data.</div>";

        const float width = 980;
        const float height = 560;
        const float margin = 36;

        var minX = nodes.Min(n => n.X);
        var maxX = nodes.Max(n => n.X);
        var minY = nodes.Min(n => n.Y);
        var maxY = nodes.Max(n => n.Y);

        if (Math.Abs(maxX - minX) < 0.001f)
        {
            maxX += 1f;
            minX -= 1f;
        }

        if (Math.Abs(maxY - minY) < 0.001f)
        {
            maxY += 1f;
            minY -= 1f;
        }

        float ScaleX(float value) => margin + ((value - minX) / (maxX - minX)) * (width - margin * 2);
        float ScaleY(float value) => margin + ((value - minY) / (maxY - minY)) * (height - margin * 2);

        static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        var builder = new StringBuilder();
        builder.AppendLine($"<svg class='topology-svg' viewBox='0 0 {F(width)} {F(height)}' role='img' aria-label='Network topology snapshot'>");
        builder.AppendLine("  <rect x='0' y='0' width='100%' height='100%' fill='#f8fafd' />");

        foreach (var edge in edges)
        {
            var a = nodes[edge.A];
            var b = nodes[edge.B];
            builder.AppendLine(
                "  <line " +
                $"x1='{F(ScaleX(a.X))}' y1='{F(ScaleY(a.Y))}' " +
                $"x2='{F(ScaleX(b.X))}' y2='{F(ScaleY(b.Y))}' " +
                "stroke='#94a3b8' stroke-width='1.2' stroke-opacity='0.8' />");
        }

        var labelStep = Math.Max(1, nodes.Count / 18);

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var cx = ScaleX(node.X);
            var cy = ScaleY(node.Y);
            var fill = node.Kind switch
            {
                BenchmarkDeviceKind.Hub => "#dc2626",
                BenchmarkDeviceKind.Emitter => "#0f766e",
                _ => "#2563eb",
            };

            builder.AppendLine(
                "  <circle " +
                $"cx='{F(cx)}' cy='{F(cy)}' r='5.6' fill='{fill}' stroke='#ffffff' stroke-width='1.1' />");

            var shouldLabel = node.Kind == BenchmarkDeviceKind.Hub || i % labelStep == 0;
            if (!shouldLabel)
                continue;

            builder.AppendLine(
                "  <text " +
                $"x='{F(cx + 8)}' y='{F(cy - 8)}' " +
                "font-size='10' font-family='Segoe UI, sans-serif' fill='#334155'>" +
                $"{WebUtility.HtmlEncode(node.Name)}" +
                "</text>");
        }

        builder.AppendLine("  <g transform='translate(14, 14)'>");
        builder.AppendLine("    <rect x='0' y='0' width='216' height='72' rx='8' fill='rgba(255,255,255,0.9)' stroke='#cbd5e1' />");
        builder.AppendLine("    <circle cx='16' cy='20' r='5' fill='#dc2626' /><text x='30' y='24' font-size='11' fill='#334155'>Hub</text>");
        builder.AppendLine("    <circle cx='16' cy='38' r='5' fill='#2563eb' /><text x='30' y='42' font-size='11' fill='#334155'>Generator</text>");
        builder.AppendLine("    <circle cx='16' cy='56' r='5' fill='#0f766e' /><text x='30' y='60' font-size='11' fill='#334155'>Emitter</text>");
        builder.AppendLine($"    <text x='122' y='24' font-size='11' fill='#334155'>Nodes: {nodes.Count}</text>");
        builder.AppendLine($"    <text x='122' y='42' font-size='11' fill='#334155'>Links: {edges.Count}</text>");
        builder.AppendLine("  </g>");

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static string FormatNumber(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
