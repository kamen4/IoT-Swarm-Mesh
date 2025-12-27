using Core.Services;
using System.Globalization;
using System.Text;

namespace Core.Statistics;

/// <summary>
/// Генератор HTML отчёта со статистикой и графиками
/// </summary>
public class ReportGenerator
{
    public static string GenerateHtmlReport(SimulationStatistics stats)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='ru'>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset='UTF-8'>");
        sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine("    <title>IoT Swarm Simulation Report</title>");
        sb.AppendLine("    <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetStyles());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class='container'>");
        
        // Header
        sb.AppendLine("<h1>?? IoT Swarm Simulation Report</h1>");
        
        // Summary cards
        sb.AppendLine("<div class='summary-grid'>");
        AddStatCard(sb, "?? Создано пакетов", stats.TotalPacketsCreated.ToString());
        AddStatCard(sb, "? Доставлено", stats.TotalPacketsDelivered.ToString());
        AddStatCard(sb, "? Потеряно", stats.TotalPacketsLost.ToString());
        AddStatCard(sb, "?? Dropped", stats.TotalPacketsDropped.ToString());
        AddStatCard(sb, "?? Forwarded", stats.TotalPacketsForwarded.ToString());
        AddStatCard(sb, "?? Хопов", stats.TotalHops.ToString());
        
        var deliveryClass = stats.DeliveryRate > 80 ? "good" : stats.DeliveryRate > 50 ? "warning" : "bad";
        AddStatCard(sb, "?? Доставляемость", $"<span class='{deliveryClass}'>{stats.DeliveryRate:F1}%</span>");
        
        var lossClass = stats.LossRate < 10 ? "good" : stats.LossRate < 30 ? "warning" : "bad";
        AddStatCard(sb, "?? Потери", $"<span class='{lossClass}'>{stats.LossRate:F1}%</span>");
        sb.AppendLine("</div>");
        
        // Time statistics
        sb.AppendLine("<h2>?? Временные характеристики</h2>");
        sb.AppendLine("<div class='summary-grid'>");
        AddStatCard(sb, "Среднее время доставки", $"{stats.AverageDeliveryTimeMs:F0} мс");
        AddStatCard(sb, "Минимальное время", $"{(stats.MinDeliveryTimeMs == double.MaxValue ? 0 : stats.MinDeliveryTimeMs):F0} мс");
        AddStatCard(sb, "Максимальное время", $"{stats.MaxDeliveryTimeMs:F0} мс");
        AddStatCard(sb, "Пакетов/сек", $"{stats.PacketsPerSecond:F1}");
        AddStatCard(sb, "Средн. хопов", $"{stats.AverageHopsPerPacket:F1}");
        AddStatCard(sb, "Средн. дистанция хопа", $"{stats.AverageHopDistance:F0}");
        sb.AppendLine("</div>");
        
        // Charts
        sb.AppendLine("<h2>?? Графики</h2>");
        sb.AppendLine("<div class='chart-row'>");
        
        // Pie chart - packet status
        sb.AppendLine("<div class='chart-container'>");
        sb.AppendLine("<canvas id='packetStatusChart'></canvas>");
        sb.AppendLine("</div>");
        
        // Bar chart - device stats
        sb.AppendLine("<div class='chart-container'>");
        sb.AppendLine("<canvas id='deviceStatsChart'></canvas>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("</div>");
        
        // Delivery time histogram
        sb.AppendLine("<div class='chart-container'>");
        sb.AppendLine("<canvas id='deliveryTimeChart'></canvas>");
        sb.AppendLine("</div>");
        
        // Device statistics table
        if (stats.DeviceStats.Count > 0)
        {
            sb.AppendLine("<h2>??? Статистика по устройствам</h2>");
            sb.AppendLine("<div class='chart-container'>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ID</th><th>Имя</th><th>Отправлено</th><th>Получено</th><th>Переслано</th><th>Dropped</th><th>Разряд батареи</th></tr>");
            
            foreach (var (deviceId, deviceStats) in stats.DeviceStats)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "<tr><td>{0:N}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6:F4}</td></tr>",
                    deviceId,
                    deviceStats.DeviceName,
                    deviceStats.PacketsSent,
                    deviceStats.PacketsReceived,
                    deviceStats.PacketsForwarded,
                    deviceStats.PacketsDropped,
                    deviceStats.TotalBatteryDrain));
            }
            
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }
        
        // Timestamp
        sb.AppendLine($"<p class='timestamp'>Отчёт сгенерирован: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"<p class='timestamp'>Длительность симуляции: {stats.SimulationDuration.TotalSeconds:F1} сек</p>");
        
        sb.AppendLine("</div>"); // container
        
        // JavaScript for charts
        sb.AppendLine("<script>");
        AddChartScripts(sb, stats);
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }

    /// <summary>
    /// Генерирует сравнительный HTML отчёт для нескольких симуляций
    /// </summary>
    public static string GenerateComparisonReport(IReadOnlyList<SavedSimulationResult> results)
    {
        if (results.Count == 0)
            return "<html><body><h1>Нет сохранённых симуляций</h1></body></html>";
        
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='ru'>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset='UTF-8'>");
        sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine("    <title>IoT Swarm - Сравнение симуляций</title>");
        sb.AppendLine("    <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetStyles());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class='container'>");
        
        sb.AppendLine("<h1>?? Сравнение симуляций</h1>");
        sb.AppendLine($"<p class='timestamp'>Всего симуляций: {results.Count}</p>");
        
        // Comparison table
        sb.AppendLine("<h2>?? Сводная таблица</h2>");
        sb.AppendLine("<div class='chart-container' style='overflow-x: auto;'>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Имя</th><th>Протокол</th><th>Интервал</th><th>Loss Rate</th><th>Создано</th><th>Доставлено</th><th>Потеряно</th><th>Доставляемость</th><th>Avg Hops</th><th>Avg Time</th></tr>");
        
        foreach (var result in results)
        {
            var s = result.Statistics;
            var c = result.Config;
            var deliveryClass = s.DeliveryRate > 80 ? "good" : s.DeliveryRate > 50 ? "warning" : "bad";
            
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "<tr><td>{0}</td><td>{1}</td><td>{2}ms</td><td>{3:P1}</td><td>{4}</td><td>{5}</td><td>{6}</td><td class='{7}'>{8:F1}%</td><td>{9:F1}</td><td>{10:F0}ms</td></tr>",
                result.Name,
                c.ProtocolName,
                c.PacketIntervalMs,
                c.BasePacketLossRate,
                s.TotalPacketsCreated,
                s.TotalPacketsDelivered,
                s.TotalPacketsLost,
                deliveryClass,
                s.DeliveryRate,
                s.AverageHopsPerPacket,
                s.AverageDeliveryTimeMs));
        }
        
        sb.AppendLine("</table>");
        sb.AppendLine("</div>");
        
        // Comparison charts
        sb.AppendLine("<h2>?? Сравнительные графики</h2>");
        sb.AppendLine("<div class='chart-row'>");
        
        sb.AppendLine("<div class='chart-container'>");
        sb.AppendLine("<canvas id='deliveryRateChart'></canvas>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("<div class='chart-container'>");
        sb.AppendLine("<canvas id='avgTimeChart'></canvas>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("</div>");
        
        sb.AppendLine("<div class='chart-row'>");
        
        sb.AppendLine("<div class='chart-container'>");
        sb.AppendLine("<canvas id='packetsChart'></canvas>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("<div class='chart-container'>");
        sb.AppendLine("<canvas id='hopsChart'></canvas>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("</div>");
        
        // Individual results
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            sb.AppendLine($"<h2>?? {result.Name}</h2>");
            sb.AppendLine("<div class='summary-grid'>");
            AddStatCard(sb, "Протокол", result.Config.ProtocolName);
            AddStatCard(sb, "Создано", result.Statistics.TotalPacketsCreated.ToString());
            AddStatCard(sb, "Доставлено", result.Statistics.TotalPacketsDelivered.ToString());
            AddStatCard(sb, "Потеряно", result.Statistics.TotalPacketsLost.ToString());
            AddStatCard(sb, "Доставляемость", $"{result.Statistics.DeliveryRate:F1}%");
            AddStatCard(sb, "Avg Time", $"{result.Statistics.AverageDeliveryTimeMs:F0}ms");
            sb.AppendLine("</div>");
        }
        
        sb.AppendLine($"<p class='timestamp'>Отчёт сгенерирован: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        
        sb.AppendLine("</div>"); // container
        
        // JavaScript for comparison charts
        sb.AppendLine("<script>");
        AddComparisonChartScripts(sb, results);
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }

    private static string GetStyles()
    {
        return @"
        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .container { max-width: 1400px; margin: 0 auto; }
        h1 { color: #333; text-align: center; margin-bottom: 30px; }
        h2 { color: #555; border-bottom: 2px solid #4a90d9; padding-bottom: 10px; }
        .summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 15px; margin-bottom: 30px; }
        .stat-card { background: white; padding: 15px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); text-align: center; }
        .stat-value { font-size: 1.5em; font-weight: bold; color: #4a90d9; }
        .stat-label { color: #666; margin-top: 5px; font-size: 0.9em; }
        .chart-container { background: white; padding: 20px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); margin-bottom: 20px; }
        .chart-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background: #4a90d9; color: white; }
        tr:hover { background: #f5f5f5; }
        .good { color: #28a745; font-weight: bold; }
        .warning { color: #ffc107; font-weight: bold; }
        .bad { color: #dc3545; font-weight: bold; }
        .timestamp { color: #999; font-size: 0.9em; text-align: center; margin-top: 20px; }
        ";
    }
    
    private static void AddStatCard(StringBuilder sb, string label, string value)
    {
        sb.AppendLine($"<div class='stat-card'><div class='stat-value'>{value}</div><div class='stat-label'>{label}</div></div>");
    }

    private static void AddChartScripts(StringBuilder sb, SimulationStatistics stats)
    {
        // Packet status pie chart
        sb.AppendLine(@"
        new Chart(document.getElementById('packetStatusChart'), {
            type: 'doughnut',
            data: {
                labels: ['Доставлено', 'Потеряно', 'Dropped'],
                datasets: [{
                    data: [" + stats.TotalPacketsDelivered + ", " + stats.TotalPacketsLost + ", " + stats.TotalPacketsDropped + @"],
                    backgroundColor: ['#28a745', '#dc3545', '#ffc107']
                }]
            },
            options: {
                responsive: true,
                plugins: { title: { display: true, text: 'Статус пакетов' } }
            }
        });");
        
        // Device stats bar chart
        var deviceNames = stats.DeviceStats.Values.Select(d => $"'{d.DeviceName}'");
        var deviceSent = stats.DeviceStats.Values.Select(d => d.PacketsSent.ToString());
        var deviceReceived = stats.DeviceStats.Values.Select(d => d.PacketsReceived.ToString());
        var deviceForwarded = stats.DeviceStats.Values.Select(d => d.PacketsForwarded.ToString());
        
        sb.AppendLine(@"
        new Chart(document.getElementById('deviceStatsChart'), {
            type: 'bar',
            data: {
                labels: [" + string.Join(",", deviceNames) + @"],
                datasets: [
                    { label: 'Отправлено', data: [" + string.Join(",", deviceSent) + @"], backgroundColor: '#4a90d9' },
                    { label: 'Получено', data: [" + string.Join(",", deviceReceived) + @"], backgroundColor: '#28a745' },
                    { label: 'Переслано', data: [" + string.Join(",", deviceForwarded) + @"], backgroundColor: '#ffc107' }
                ]
            },
            options: {
                responsive: true,
                plugins: { title: { display: true, text: 'Статистика по устройствам' } },
                scales: { y: { beginAtZero: true } }
            }
        });");
        
        // Delivery time line chart
        var deliveryTimes = stats.PacketLog
            .Where(p => p.DeliveryTimeMs.HasValue)
            .Select((p, i) => new { Index = i, Time = p.DeliveryTimeMs!.Value })
            .TakeLast(50)
            .ToList();
        
        if (deliveryTimes.Count > 0)
        {
            var labels = string.Join(",", deliveryTimes.Select(d => d.Index));
            var times = string.Join(",", deliveryTimes.Select(d => d.Time.ToString("F0", CultureInfo.InvariantCulture)));
            
            sb.AppendLine(@"
            new Chart(document.getElementById('deliveryTimeChart'), {
                type: 'line',
                data: {
                    labels: [" + labels + @"],
                    datasets: [{
                        label: 'Время доставки (мс)',
                        data: [" + times + @"],
                        borderColor: '#4a90d9',
                        tension: 0.1,
                        fill: false
                    }]
                },
                options: {
                    responsive: true,
                    plugins: { title: { display: true, text: 'Время доставки пакетов' } },
                    scales: { y: { beginAtZero: true } }
                }
            });");
        }
    }

    private static void AddComparisonChartScripts(StringBuilder sb, IReadOnlyList<SavedSimulationResult> results)
    {
        var names = string.Join(",", results.Select(r => $"'{r.Name}'"));
        var deliveryRates = string.Join(",", results.Select(r => r.Statistics.DeliveryRate.ToString("F1", CultureInfo.InvariantCulture)));
        var avgTimes = string.Join(",", results.Select(r => r.Statistics.AverageDeliveryTimeMs.ToString("F0", CultureInfo.InvariantCulture)));
        var created = string.Join(",", results.Select(r => r.Statistics.TotalPacketsCreated));
        var delivered = string.Join(",", results.Select(r => r.Statistics.TotalPacketsDelivered));
        var lost = string.Join(",", results.Select(r => r.Statistics.TotalPacketsLost));
        var avgHops = string.Join(",", results.Select(r => r.Statistics.AverageHopsPerPacket.ToString("F1", CultureInfo.InvariantCulture)));
        
        sb.AppendLine(@"
        new Chart(document.getElementById('deliveryRateChart'), {
            type: 'bar',
            data: {
                labels: [" + names + @"],
                datasets: [{ label: 'Доставляемость (%)', data: [" + deliveryRates + @"], backgroundColor: '#28a745' }]
            },
            options: {
                responsive: true,
                plugins: { title: { display: true, text: 'Доставляемость по симуляциям' } },
                scales: { y: { beginAtZero: true, max: 100 } }
            }
        });

        new Chart(document.getElementById('avgTimeChart'), {
            type: 'bar',
            data: {
                labels: [" + names + @"],
                datasets: [{ label: 'Среднее время (мс)', data: [" + avgTimes + @"], backgroundColor: '#4a90d9' }]
            },
            options: {
                responsive: true,
                plugins: { title: { display: true, text: 'Среднее время доставки' } },
                scales: { y: { beginAtZero: true } }
            }
        });

        new Chart(document.getElementById('packetsChart'), {
            type: 'bar',
            data: {
                labels: [" + names + @"],
                datasets: [
                    { label: 'Создано', data: [" + created + @"], backgroundColor: '#4a90d9' },
                    { label: 'Доставлено', data: [" + delivered + @"], backgroundColor: '#28a745' },
                    { label: 'Потеряно', data: [" + lost + @"], backgroundColor: '#dc3545' }
                ]
            },
            options: {
                responsive: true,
                plugins: { title: { display: true, text: 'Пакеты по симуляциям' } },
                scales: { y: { beginAtZero: true } }
            }
        });

        new Chart(document.getElementById('hopsChart'), {
            type: 'bar',
            data: {
                labels: [" + names + @"],
                datasets: [{ label: 'Среднее кол-во хопов', data: [" + avgHops + @"], backgroundColor: '#ffc107' }]
            },
            options: {
                responsive: true,
                plugins: { title: { display: true, text: 'Среднее количество хопов' } },
                scales: { y: { beginAtZero: true } }
            }
        });
        ");
    }
}
