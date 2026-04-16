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
/// Writes self-contained HTML benchmark reports with summary tables,
/// topology visualization, and interactive charts.
/// </summary>
internal static class HtmlReportWriter
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
    /// Generates an HTML report file for one benchmark scenario.
    /// </summary>
    /// <param name="outputDirectory">Directory where report files are written.</param>
    /// <param name="scenario">Scenario metadata and applied runtime settings.</param>
    /// <param name="session">Benchmark session data.</param>
    /// <returns>Absolute path to the generated report file.</returns>
    public static async Task<string> WriteAsync(
        string outputDirectory,
        BenchmarkScenario scenario,
        BenchmarkSession session)
    {
        Directory.CreateDirectory(outputDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var filePath = Path.Combine(outputDirectory, $"{scenario.Slug}_{timestamp}_report.html");

        var encodedName = WebUtility.HtmlEncode(scenario.Name);
        var encodedDescription = WebUtility.HtmlEncode(scenario.Description);
        var encodedTopology = WebUtility.HtmlEncode(scenario.TopologyBuilder.Name);

        var generatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

        var stats = BuildScenarioStats(scenario.Config);
        var topologySnapshot = BuildTopologySnapshot(scenario);
        var topologySvg = BuildTopologySvg(topologySnapshot.Nodes, topologySnapshot.Edges);

        var metricRows = BuildMetricRows(session.Results);
        var derivedMetricRows = BuildDerivedMetricRows(session.Results);
        var vectorRows = BuildVectorRows(scenario.Vector);
        var scenarioRows = BuildScenarioRows(stats);
        var eventRows = BuildEventRows(scenario.Config.Events, maxRows: 18);

        var finalMetricsPayload = session.Results
            .Select(r => new
            {
                routerName = r.RouterName,
                totalPacketsRegistered = r.TotalPacketsRegistered,
                totalPacketsDelivered = r.TotalPacketsDelivered,
                totalPacketsExpired = r.TotalPacketsExpired,
                duplicateDeliveries = r.DuplicateDeliveries,
                duplicateRate = r.TotalPacketsDelivered <= 0
                    ? 0
                    : (r.DuplicateDeliveries / r.TotalPacketsDelivered) * 100.0,
                expireRate = r.TotalPacketsRegistered <= 0
                    ? 0
                    : (r.TotalPacketsExpired / r.TotalPacketsRegistered) * 100.0,
                avgHopCount = r.AvgHopCount,
                deliveryRate = r.DeliveryRate,
                avgTickMs = r.AvgTickMs,
            })
            .ToArray();

        var historyPayload = session.Results
            .Select(r => new
            {
                routerName = r.RouterName,
                points = r.History.Select(h => new
                {
                    tick = h.Tick,
                    activePackets = h.ActivePackets,
                    totalDelivered = h.TotalDelivered,
                    totalExpired = h.TotalExpired,
                    deliveryRate = h.DeliveryRate,
                    duplicateDeliveries = h.DuplicateDeliveries,
                    avgHopCount = h.AvgHopCount,
                    tickMs = h.TickMs,
                }),
            })
            .ToArray();

        var metricsJson = JsonSerializer.Serialize(finalMetricsPayload, JsonOptions);
        var historyJson = JsonSerializer.Serialize(historyPayload, JsonOptions);

        var seedText = scenario.SeedTemplate?.Seed > 0
            ? scenario.SeedTemplate.Seed.ToString(CultureInfo.InvariantCulture)
            : "n/a";

        var html = $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{{encodedName}} - Benchmark Report</title>
  <style>
    :root {
      --bg: #f3f5f8;
      --panel: #ffffff;
      --ink: #0f172a;
      --muted: #475569;
      --line: #d8dee8;
      --accent: #0f766e;
      --accent-soft: #14b8a6;
      --warn: #b45309;
      --danger: #b91c1c;
      --radius: 14px;
    }

    * {
      box-sizing: border-box;
    }

    body {
      margin: 0;
      padding: 24px;
      font-family: "Segoe UI", Tahoma, sans-serif;
      color: var(--ink);
      background: radial-gradient(circle at top right, #dff6f0 0%, var(--bg) 46%), var(--bg);
    }

    .wrap {
      max-width: 1320px;
      margin: 0 auto;
      display: grid;
      gap: 20px;
    }

    .hero {
      background: linear-gradient(128deg, #0f766e 0%, #0b5e59 55%, #0f3f47 100%);
      color: #ecfeff;
      border-radius: var(--radius);
      padding: 24px;
      box-shadow: 0 12px 28px rgba(15, 23, 42, 0.20);
    }

    .hero h1 {
      margin: 0;
      font-size: 1.7rem;
    }

    .hero p {
      margin: 10px 0 0;
      color: #d9faf4;
      line-height: 1.45;
    }

    .meta-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
      gap: 12px;
      margin-top: 16px;
    }

    .meta {
      background: rgba(255, 255, 255, 0.10);
      border: 1px solid rgba(255, 255, 255, 0.14);
      border-radius: 10px;
      padding: 10px 12px;
      display: grid;
      gap: 6px;
    }

    .meta b {
      font-size: 0.84rem;
      color: #b9f8ef;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    .panel {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: var(--radius);
      padding: 16px;
      box-shadow: 0 8px 20px rgba(15, 23, 42, 0.06);
    }

    .panel h2 {
      margin: 0 0 10px;
      font-size: 1.08rem;
    }

    .tables {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 18px;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.9rem;
    }

    thead th {
      text-align: left;
      font-size: 0.78rem;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--muted);
      border-bottom: 1px solid var(--line);
      padding: 10px 8px;
    }

    tbody td {
      border-bottom: 1px solid #ebeff5;
      padding: 9px 8px;
      vertical-align: top;
    }

    tbody tr:last-child td {
      border-bottom: none;
    }

    .mono {
      font-family: "Cascadia Code", "Consolas", monospace;
      font-size: 0.83rem;
    }

    .topology-wrap {
      border: 1px solid #dbe4ef;
      border-radius: 12px;
      overflow: hidden;
      background: #f8fafc;
    }

    .topology-svg {
      display: block;
      width: 100%;
      height: auto;
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 18px;
    }

    canvas {
      width: 100%;
      max-height: 320px;
    }

    .foot {
      color: var(--muted);
      font-size: 0.82rem;
    }
  </style>
</head>
<body>
  <div class="wrap">
    <section class="hero">
      <h1>{{encodedName}}</h1>
      <p>{{encodedDescription}}</p>
      <div class="meta-grid">
        <div class="meta">
          <b>Generated</b>
          <span class="mono">{{generatedAt}}</span>
        </div>
        <div class="meta">
          <b>Topology Builder</b>
          <span>{{encodedTopology}}</span>
        </div>
        <div class="meta">
          <b>Visibility / Duration</b>
          <span>{{scenario.Config.VisibilityDistance}} units / {{scenario.Config.DurationTicks}} ticks</span>
        </div>
        <div class="meta">
          <b>Packet Defaults</b>
          <span>TTL {{scenario.Config.DefaultTtl}}, travel {{scenario.Config.TicksToTravel}} ticks</span>
        </div>
        <div class="meta">
          <b>Routers / Seed</b>
          <span>{{stats.RouterCount}} routers, seed {{seedText}}</span>
        </div>
        <div class="meta">
          <b>Nodes / Events</b>
          <span>{{stats.TotalNodes}} nodes, {{stats.EventCount}} events</span>
        </div>
      </div>
    </section>

    <section class="tables">
      <div class="panel">
        <h2>Final Router Metrics</h2>
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
          <tbody>
{{metricRows}}
          </tbody>
        </table>
      </div>

      <div class="panel">
        <h2>Derived Router Metrics</h2>
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
          <tbody>
{{derivedMetricRows}}
          </tbody>
        </table>
      </div>

      <div class="panel">
        <h2>Scenario Composition</h2>
        <table>
          <tbody>
{{scenarioRows}}
          </tbody>
        </table>
      </div>

      <div class="panel">
        <h2>Swarm Vector</h2>
        <table>
          <tbody>
{{vectorRows}}
          </tbody>
        </table>
      </div>
    </section>

    <section class="panel">
      <h2>Initial Topology Snapshot</h2>
      <p class="foot">Nodes: {{stats.TotalNodes}}, links: {{topologySnapshot.Edges.Count}}, builder: {{encodedTopology}}</p>
      <div class="topology-wrap">
{{topologySvg}}
      </div>
    </section>

    <section class="panel">
      <h2>Event Timeline Preview</h2>
      <table>
        <thead>
          <tr>
            <th>Tick</th>
            <th>Event</th>
          </tr>
        </thead>
        <tbody>
{{eventRows}}
        </tbody>
      </table>
    </section>

    <section class="grid">
      <div class="panel">
        <h2>Delivery Rate by Router</h2>
        <canvas id="deliveryBar"></canvas>
      </div>
      <div class="panel">
        <h2>Packet Volume by Router</h2>
        <canvas id="packetVolumeBar"></canvas>
      </div>
      <div class="panel">
        <h2>Duplicate and Expire Rates</h2>
        <canvas id="duplicateRateBar"></canvas>
      </div>
      <div class="panel">
        <h2>Hop Count and Tick Cost</h2>
        <canvas id="efficiencyBar"></canvas>
      </div>
      <div class="panel">
        <h2>Delivery Rate over Time</h2>
        <canvas id="deliveryTimeline"></canvas>
      </div>
      <div class="panel">
        <h2>Active Packets over Time</h2>
        <canvas id="activeTimeline"></canvas>
      </div>
      <div class="panel">
        <h2>Tick Duration over Time</h2>
        <canvas id="tickTimeline"></canvas>
      </div>
      <div class="panel">
        <h2>Duplicate Deliveries over Time</h2>
        <canvas id="duplicateTimeline"></canvas>
      </div>
    </section>

    <section class="panel foot">
      Report generated by ConsoleUI benchmark runner. Data source: BenchmarkSession JSON emitted by Engine.Benchmark.
    </section>
  </div>

  <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
  <script>
    const metrics = {{metricsJson}};
    const historySeries = {{historyJson}};

    const colors = [
      "#0f766e",
      "#2563eb",
      "#f59e0b",
      "#dc2626",
      "#7c3aed",
      "#0ea5e9"
    ];

    function colorFor(index) {
      return colors[index % colors.length];
    }

    function lineSeries(series, index, mapper, label, yAxisId = "y") {
      return {
        label,
        data: series.points.map(p => ({ x: p.tick, y: mapper(p) })),
        borderColor: colorFor(index),
        backgroundColor: colorFor(index),
        borderWidth: 2,
        pointRadius: 0,
        tension: 0.15,
        yAxisID: yAxisId
      };
    }

    new Chart(document.getElementById("deliveryBar"), {
      type: "bar",
      data: {
        labels: metrics.map(m => m.routerName),
        datasets: [{
          label: "Delivery rate (%)",
          data: metrics.map(m => m.deliveryRate),
          backgroundColor: metrics.map((_, i) => colorFor(i))
        }]
      },
      options: {
        responsive: true,
        scales: {
          y: { beginAtZero: true, max: 100 }
        }
      }
    });

    new Chart(document.getElementById("packetVolumeBar"), {
      type: "bar",
      data: {
        labels: metrics.map(m => m.routerName),
        datasets: [
          {
            label: "Registered",
            data: metrics.map(m => m.totalPacketsRegistered),
            backgroundColor: "#334155"
          },
          {
            label: "Delivered",
            data: metrics.map(m => m.totalPacketsDelivered),
            backgroundColor: "#16a34a"
          },
          {
            label: "Expired",
            data: metrics.map(m => m.totalPacketsExpired),
            backgroundColor: "#dc2626"
          }
        ]
      },
      options: {
        responsive: true,
        scales: {
          y: { beginAtZero: true }
        }
      }
    });

    new Chart(document.getElementById("duplicateRateBar"), {
      type: "bar",
      data: {
        labels: metrics.map(m => m.routerName),
        datasets: [
          {
            label: "Duplicate deliveries",
            data: metrics.map(m => m.duplicateDeliveries),
            backgroundColor: "#f59e0b",
            yAxisID: "y"
          },
          {
            type: "line",
            label: "Duplicate rate (% of delivered)",
            data: metrics.map(m => m.duplicateRate),
            borderColor: "#b91c1c",
            backgroundColor: "#b91c1c",
            borderWidth: 2,
            pointRadius: 3,
            tension: 0.2,
            yAxisID: "y1"
          },
          {
            type: "line",
            label: "Expire rate (% of registered)",
            data: metrics.map(m => m.expireRate),
            borderColor: "#1d4ed8",
            backgroundColor: "#1d4ed8",
            borderWidth: 2,
            pointRadius: 3,
            tension: 0.2,
            yAxisID: "y1"
          }
        ]
      },
      options: {
        responsive: true,
        scales: {
          y: { beginAtZero: true, position: "left" },
          y1: { beginAtZero: true, position: "right", grid: { drawOnChartArea: false } }
        }
      }
    });

    new Chart(document.getElementById("efficiencyBar"), {
      data: {
        labels: metrics.map(m => m.routerName),
        datasets: [
          {
            type: "bar",
            label: "Avg hop count",
            data: metrics.map(m => m.avgHopCount),
            backgroundColor: "#0f766e",
            yAxisID: "y"
          },
          {
            type: "line",
            label: "Avg tick ms",
            data: metrics.map(m => m.avgTickMs),
            borderColor: "#1d4ed8",
            backgroundColor: "#1d4ed8",
            borderWidth: 2,
            pointRadius: 3,
            tension: 0.2,
            yAxisID: "y1"
          }
        ]
      },
      options: {
        responsive: true,
        scales: {
          y: { beginAtZero: true, position: "left" },
          y1: { beginAtZero: true, position: "right", grid: { drawOnChartArea: false } }
        }
      }
    });

    new Chart(document.getElementById("deliveryTimeline"), {
      type: "line",
      data: {
        datasets: historySeries.map((series, index) =>
          lineSeries(series, index, p => p.deliveryRate, series.routerName)
        )
      },
      options: {
        responsive: true,
        parsing: false,
        scales: {
          x: { type: "linear", title: { display: true, text: "Tick" } },
          y: { beginAtZero: true, max: 100, title: { display: true, text: "Delivery %" } }
        }
      }
    });

    new Chart(document.getElementById("activeTimeline"), {
      type: "line",
      data: {
        datasets: historySeries.map((series, index) =>
          lineSeries(series, index, p => p.activePackets, series.routerName)
        )
      },
      options: {
        responsive: true,
        parsing: false,
        scales: {
          x: { type: "linear", title: { display: true, text: "Tick" } },
          y: { beginAtZero: true, title: { display: true, text: "Active packets" } }
        }
      }
    });

    new Chart(document.getElementById("tickTimeline"), {
      type: "line",
      data: {
        datasets: historySeries.map((series, index) =>
          lineSeries(series, index, p => p.tickMs, series.routerName)
        )
      },
      options: {
        responsive: true,
        parsing: false,
        scales: {
          x: { type: "linear", title: { display: true, text: "Tick" } },
          y: { beginAtZero: true, title: { display: true, text: "Tick ms" } }
        }
      }
    });

    new Chart(document.getElementById("duplicateTimeline"), {
      type: "line",
      data: {
        datasets: historySeries.map((series, index) =>
          lineSeries(series, index, p => p.duplicateDeliveries, series.routerName)
        )
      },
      options: {
        responsive: true,
        parsing: false,
        scales: {
          x: { type: "linear", title: { display: true, text: "Tick" } },
          y: { beginAtZero: true, title: { display: true, text: "Duplicate deliveries" } }
        }
      }
    });
  </script>
</body>
</html>
""";

        await File.WriteAllTextAsync(filePath, html);
        return filePath;
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

    private static string BuildMetricRows(IEnumerable<BenchmarkResult> results)
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

    private static string BuildDerivedMetricRows(IEnumerable<BenchmarkResult> results)
    {
        var rows = results.Select(result =>
        {
            var uniqueDelivered = Math.Max(0.0, result.TotalPacketsDelivered - result.DuplicateDeliveries);
            var duplicateRate = result.TotalPacketsDelivered <= 0
                ? 0
                : (result.DuplicateDeliveries / result.TotalPacketsDelivered) * 100.0;
            var expireRate = result.TotalPacketsRegistered <= 0
                ? 0
                : (result.TotalPacketsExpired / result.TotalPacketsRegistered) * 100.0;
            var registerPerDelivered = result.TotalPacketsDelivered <= 0
                ? 0
                : result.TotalPacketsRegistered / result.TotalPacketsDelivered;

            return "            <tr>" + Environment.NewLine +
                   $"              <td>{WebUtility.HtmlEncode(result.RouterName)}</td>" + Environment.NewLine +
                   $"              <td class=\"mono\">{FormatNumber(uniqueDelivered)}</td>" + Environment.NewLine +
                   $"              <td class=\"mono\">{FormatNumber(duplicateRate)}%</td>" + Environment.NewLine +
                   $"              <td class=\"mono\">{FormatNumber(expireRate)}%</td>" + Environment.NewLine +
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
                $"<td>{WebUtility.HtmlEncode(DescribeEvent(entry.Event))}</td>" +
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
            return "<div class='foot' style='padding:16px'>No topology data.</div>";

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
        builder.AppendLine("  <rect x='0' y='0' width='100%' height='100%' fill='#f8fafc' />");

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
        builder.AppendLine("    <rect x='0' y='0' width='212' height='72' rx='8' fill='rgba(255,255,255,0.88)' stroke='#cbd5e1' />");
        builder.AppendLine("    <circle cx='16' cy='20' r='5' fill='#dc2626' /><text x='30' y='24' font-size='11' fill='#334155'>Hub</text>");
        builder.AppendLine("    <circle cx='16' cy='38' r='5' fill='#2563eb' /><text x='30' y='42' font-size='11' fill='#334155'>Generator</text>");
        builder.AppendLine("    <circle cx='16' cy='56' r='5' fill='#0f766e' /><text x='30' y='60' font-size='11' fill='#334155'>Emitter</text>");
        builder.AppendLine($"    <text x='118' y='24' font-size='11' fill='#334155'>Nodes: {nodes.Count}</text>");
        builder.AppendLine($"    <text x='118' y='42' font-size='11' fill='#334155'>Links: {edges.Count}</text>");
        builder.AppendLine("  </g>");

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static string FormatNumber(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
