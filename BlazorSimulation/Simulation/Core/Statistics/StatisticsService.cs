using System.Globalization;
using System.Text;
using Core.Services;

namespace Core.Statistics;

public class StatisticsService : IStatisticsService
{
    private readonly object _lock = new();
    private SimulationStatistics _current = new() { StartTime = DateTime.UtcNow };
    private readonly List<double> _deliveryTimes = [];
    private readonly List<SavedSimulationResult> _savedResults = [];

    public SimulationStatistics Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    public IReadOnlyList<SavedSimulationResult> SavedResults
    {
        get
        {
            lock (_lock)
            {
                return _savedResults.AsReadOnly();
            }
        }
    }

    public void RecordPacketCreated(Packet packet)
    {
        lock (_lock)
        {
            _current.TotalPacketsCreated++;
            
            var entry = new PacketLogEntry
            {
                PacketId = packet.Id,
                IdempotencyId = packet.IdempotencyId,
                PacketType = packet.PacketType.ToString(),
                SenderId = packet.Sender.Id,
                SenderName = packet.Sender.Name,
                ReceiverId = packet.Receiver?.Id,
                ReceiverName = packet.Receiver?.Name ?? "",
                CreatedAt = packet.CreatedOn,
                Status = "Created",
                Path = [packet.Sender.Id]
            };
            _current.PacketLog.Add(entry);
            
            // Update device stats
            EnsureDeviceStats(packet.Sender.Id, packet.Sender.Name);
            _current.DeviceStats[packet.Sender.Id].PacketsSent++;
        }
    }

    public void RecordPacketDelivered(Packet packet, TimeSpan deliveryTime)
    {
        lock (_lock)
        {
            _current.TotalPacketsDelivered++;
            
            var timeMs = deliveryTime.TotalMilliseconds;
            _deliveryTimes.Add(timeMs);
            _current.TotalDeliveryTimeMs += timeMs;
            
            _current.AverageDeliveryTimeMs = _deliveryTimes.Average();
            _current.MinDeliveryTimeMs = Math.Min(_current.MinDeliveryTimeMs, timeMs);
            _current.MaxDeliveryTimeMs = Math.Max(_current.MaxDeliveryTimeMs, timeMs);
            
            var logEntry = _current.PacketLog.FirstOrDefault(p => p.PacketId == packet.Id);
            if (logEntry is not null)
            {
                logEntry.DeliveredAt = DateTime.UtcNow;
                logEntry.DeliveryTimeMs = timeMs;
                logEntry.Status = "Delivered";
                logEntry.HopCount = packet.HopCount;
            }
            
            // Update device stats
            if (packet.Receiver is not null)
            {
                EnsureDeviceStats(packet.Receiver.Id, packet.Receiver.Name);
                _current.DeviceStats[packet.Receiver.Id].PacketsReceived++;
            }
        }
    }

    public void RecordPacketDropped(Packet packet, string reason)
    {
        lock (_lock)
        {
            _current.TotalPacketsDropped++;
            
            var logEntry = _current.PacketLog.FirstOrDefault(p => p.PacketId == packet.Id);
            if (logEntry is not null)
            {
                logEntry.Status = "Dropped";
                logEntry.DropReason = reason;
            }
        }
    }

    public void RecordPacketForwarded(Packet packet, Guid fromDeviceId, Guid toDeviceId, float distance)
    {
        lock (_lock)
        {
            _current.TotalPacketsForwarded++;
            _current.TotalDistanceTraveled += distance;
            
            var logEntry = _current.PacketLog.FirstOrDefault(p => p.IdempotencyId == packet.IdempotencyId);
            if (logEntry is not null)
            {
                logEntry.HopCount++;
                logEntry.TotalDistance += distance;
                if (!logEntry.Path.Contains(toDeviceId))
                {
                    logEntry.Path.Add(toDeviceId);
                }
            }
            
            // Update device stats
            EnsureDeviceStats(fromDeviceId, "");
            _current.DeviceStats[fromDeviceId].PacketsForwarded++;
        }
    }

    public void RecordPacketLost(Packet packet, float distance, double lossChance)
    {
        lock (_lock)
        {
            _current.TotalPacketsLost++;
            
            _current.LossLog.Add(new LossLogEntry
            {
                Timestamp = DateTime.UtcNow,
                PacketId = packet.Id,
                Distance = distance,
                LossChance = lossChance,
                FromDeviceId = packet.CurrentHop?.Id ?? Guid.Empty,
                ToDeviceId = packet.NextHop?.Id ?? Guid.Empty
            });
        }
    }

    public void RecordHop(Packet packet, Guid deviceId)
    {
        lock (_lock)
        {
            _current.TotalHops++;
            
            var hopNumber = _current.HopLog.Count(h => h.PacketId == packet.Id) + 1;
            
            _current.HopLog.Add(new HopLogEntry
            {
                Timestamp = DateTime.UtcNow,
                PacketId = packet.Id,
                FromDeviceId = packet.CurrentHop?.Id ?? Guid.Empty,
                ToDeviceId = deviceId,
                Distance = packet.CurrentHop is not null 
                    ? System.Numerics.Vector2.Distance(packet.CurrentHop.Pos, 
                        packet.NextHop?.Pos ?? packet.CurrentHop.Pos)
                    : 0,
                HopNumber = hopNumber
            });
        }
    }

    public void RecordBatteryDrain(Guid deviceId, double amount)
    {
        lock (_lock)
        {
            if (!_current.BatteryDrainByDevice.TryGetValue(deviceId, out var current))
            {
                current = 0;
            }
            _current.BatteryDrainByDevice[deviceId] = current + amount;
            
            EnsureDeviceStats(deviceId, "");
            _current.DeviceStats[deviceId].TotalBatteryDrain += amount;
        }
    }

    private void EnsureDeviceStats(Guid deviceId, string name)
    {
        if (!_current.DeviceStats.ContainsKey(deviceId))
        {
            _current.DeviceStats[deviceId] = new DeviceStatistics
            {
                DeviceId = deviceId,
                DeviceName = name
            };
        }
        else if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(_current.DeviceStats[deviceId].DeviceName))
        {
            _current.DeviceStats[deviceId].DeviceName = name;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _current = new SimulationStatistics { StartTime = DateTime.UtcNow };
            _deliveryTimes.Clear();
        }
    }

    public void SaveCurrentResult(string name, SimulationConfig config)
    {
        lock (_lock)
        {
            _current.EndTime = DateTime.UtcNow;
            _current.SimulationDuration = _current.EndTime.Value - _current.StartTime;
            
            _savedResults.Add(new SavedSimulationResult
            {
                Name = name,
                SavedAt = DateTime.UtcNow,
                Config = config,
                Statistics = _current.Clone()
            });
        }
    }

    public void RemoveSavedResult(int index)
    {
        lock (_lock)
        {
            if (index >= 0 && index < _savedResults.Count)
            {
                _savedResults.RemoveAt(index);
            }
        }
    }

    public void ClearSavedResults()
    {
        lock (_lock)
        {
            _savedResults.Clear();
        }
    }

    public string ExportToCsv()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SUMMARY ===");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"TotalPacketsCreated,{_current.TotalPacketsCreated}");
            sb.AppendLine($"TotalPacketsDelivered,{_current.TotalPacketsDelivered}");
            sb.AppendLine($"TotalPacketsDropped,{_current.TotalPacketsDropped}");
            sb.AppendLine($"TotalPacketsForwarded,{_current.TotalPacketsForwarded}");
            sb.AppendLine($"TotalPacketsLost,{_current.TotalPacketsLost}");
            sb.AppendLine($"TotalHops,{_current.TotalHops}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"DeliveryRate,{_current.DeliveryRate:F2}%");
            sb.AppendLine(CultureInfo.InvariantCulture, $"LossRate,{_current.LossRate:F2}%");
            sb.AppendLine(CultureInfo.InvariantCulture, $"AverageHopsPerPacket,{_current.AverageHopsPerPacket:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"AverageDeliveryTimeMs,{_current.AverageDeliveryTimeMs:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"MinDeliveryTimeMs,{(_current.MinDeliveryTimeMs == double.MaxValue ? 0 : _current.MinDeliveryTimeMs):F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"MaxDeliveryTimeMs,{_current.MaxDeliveryTimeMs:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"TotalDistanceTraveled,{_current.TotalDistanceTraveled:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"AverageHopDistance,{_current.AverageHopDistance:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"PacketsPerSecond,{_current.PacketsPerSecond:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"SimulationDurationMs,{_current.SimulationDuration.TotalMilliseconds:F0}");
            
            sb.AppendLine();
            sb.AppendLine("=== DEVICE STATS ===");
            sb.AppendLine("DeviceId,DeviceName,PacketsSent,PacketsReceived,PacketsForwarded,PacketsDropped,TotalBatteryDrain");
            foreach (var (deviceId, stats) in _current.DeviceStats)
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"{deviceId},{stats.DeviceName},{stats.PacketsSent},{stats.PacketsReceived},{stats.PacketsForwarded},{stats.PacketsDropped},{stats.TotalBatteryDrain:F4}");
            }
            
            sb.AppendLine();
            sb.AppendLine("=== BATTERY DRAIN ===");
            sb.AppendLine("DeviceId,BatteryDrain");
            foreach (var (deviceId, drain) in _current.BatteryDrainByDevice)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{deviceId},{drain:F4}");
            }
            
            return sb.ToString();
        }
    }

    public string ExportPacketLogToCsv()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PacketId,IdempotencyId,PacketType,SenderId,SenderName,ReceiverId,ReceiverName,CreatedAt,DeliveredAt,DeliveryTimeMs,Status,HopCount,TotalDistance,DropReason,Path");
            
            foreach (var entry in _current.PacketLog)
            {
                var path = string.Join("->", entry.Path);
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                    entry.PacketId,
                    entry.IdempotencyId,
                    entry.PacketType,
                    entry.SenderId,
                    entry.SenderName,
                    entry.ReceiverId?.ToString() ?? "",
                    entry.ReceiverName,
                    entry.CreatedAt.ToString("O"),
                    entry.DeliveredAt?.ToString("O") ?? "",
                    entry.DeliveryTimeMs?.ToString("F2") ?? "",
                    entry.Status,
                    entry.HopCount,
                    entry.TotalDistance.ToString("F2"),
                    entry.DropReason ?? "",
                    path));
            }
            
            return sb.ToString();
        }
    }

    public string ExportHopLogToCsv()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,PacketId,FromDeviceId,ToDeviceId,Distance,HopNumber");
            
            foreach (var entry in _current.HopLog)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5}",
                    entry.Timestamp.ToString("O"),
                    entry.PacketId,
                    entry.FromDeviceId,
                    entry.ToDeviceId,
                    entry.Distance.ToString("F2"),
                    entry.HopNumber));
            }
            
            sb.AppendLine();
            sb.AppendLine("=== LOSS LOG ===");
            sb.AppendLine("Timestamp,PacketId,FromDeviceId,ToDeviceId,Distance,LossChance");
            
            foreach (var entry in _current.LossLog)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5}",
                    entry.Timestamp.ToString("O"),
                    entry.PacketId,
                    entry.FromDeviceId,
                    entry.ToDeviceId,
                    entry.Distance.ToString("F2"),
                    entry.LossChance.ToString("F4")));
            }
            
            return sb.ToString();
        }
    }

    public string ExportAllResultsToCsv()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SIMULATION COMPARISON ===");
            sb.AppendLine("Name,SavedAt,Protocol,PacketInterval,Duration,PacketSpeed,PacketLoss,Created,Delivered,Lost,Dropped,DeliveryRate,LossRate,AvgHops,AvgTimeMs,MinTimeMs,MaxTimeMs");
            
            foreach (var result in _savedResults)
            {
                var s = result.Statistics;
                var c = result.Config;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11:F2},{12:F2},{13:F2},{14:F2},{15:F2},{16:F2}",
                    result.Name,
                    result.SavedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    c.ProtocolName,
                    c.PacketIntervalMs,
                    c.SimulationDurationMs,
                    c.PacketSpeed,
                    c.BasePacketLossRate,
                    s.TotalPacketsCreated,
                    s.TotalPacketsDelivered,
                    s.TotalPacketsLost,
                    s.TotalPacketsDropped,
                    s.DeliveryRate,
                    s.LossRate,
                    s.AverageHopsPerPacket,
                    s.AverageDeliveryTimeMs,
                    s.MinDeliveryTimeMs == double.MaxValue ? 0 : s.MinDeliveryTimeMs,
                    s.MaxDeliveryTimeMs));
            }
            
            return sb.ToString();
        }
    }
}
