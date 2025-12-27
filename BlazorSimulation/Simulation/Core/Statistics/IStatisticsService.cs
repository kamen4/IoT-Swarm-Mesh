using Core.Services;

namespace Core.Statistics;

/// <summary>
/// —ервис сбора статистики симул€ции
/// </summary>
public interface IStatisticsService
{
    SimulationStatistics Current { get; }
    
    /// <summary>
    /// —охранЄнные результаты симул€ций дл€ сравнени€
    /// </summary>
    IReadOnlyList<SavedSimulationResult> SavedResults { get; }
    
    void RecordPacketCreated(Packet packet);
    void RecordPacketDelivered(Packet packet, TimeSpan deliveryTime);
    void RecordPacketDropped(Packet packet, string reason);
    void RecordPacketForwarded(Packet packet, Guid fromDeviceId, Guid toDeviceId, float distance);
    void RecordPacketLost(Packet packet, float distance, double lossChance);
    void RecordBatteryDrain(Guid deviceId, double amount);
    void RecordHop(Packet packet, Guid deviceId);
    
    void Reset();
    
    /// <summary>
    /// —охранить текущие результаты с именем и параметрами
    /// </summary>
    void SaveCurrentResult(string name, SimulationConfig config);
    
    /// <summary>
    /// ”далить сохранЄнный результат
    /// </summary>
    void RemoveSavedResult(int index);
    
    /// <summary>
    /// ќчистить все сохранЄнные результаты
    /// </summary>
    void ClearSavedResults();
    
    string ExportToCsv();
    string ExportPacketLogToCsv();
    string ExportHopLogToCsv();
    
    /// <summary>
    /// Ёкспорт всех сохранЄнных симул€ций в CSV
    /// </summary>
    string ExportAllResultsToCsv();
}

/// <summary>
/// —охранЄнный результат симул€ции
/// </summary>
public class SavedSimulationResult
{
    public string Name { get; set; } = "";
    public DateTime SavedAt { get; set; }
    public SimulationConfig Config { get; set; } = new();
    public SimulationStatistics Statistics { get; set; } = new();
}

public class SimulationStatistics
{
    // Packet counts
    public int TotalPacketsCreated { get; set; }
    public int TotalPacketsDelivered { get; set; }
    public int TotalPacketsDropped { get; set; }
    public int TotalPacketsForwarded { get; set; }
    public int TotalPacketsLost { get; set; }
    public int TotalHops { get; set; }
    
    // Calculated
    public int ExtraPacketsCreated => TotalPacketsForwarded;
    public double DeliveryRate => TotalPacketsCreated > 0 
        ? (double)TotalPacketsDelivered / TotalPacketsCreated * 100 
        : 0;
    public double LossRate => (TotalPacketsCreated + TotalPacketsForwarded) > 0
        ? (double)TotalPacketsLost / (TotalPacketsCreated + TotalPacketsForwarded) * 100
        : 0;
    public double AverageHopsPerPacket => TotalPacketsDelivered > 0
        ? (double)TotalHops / TotalPacketsDelivered
        : 0;
    
    // Delivery times
    public double AverageDeliveryTimeMs { get; set; }
    public double MinDeliveryTimeMs { get; set; } = double.MaxValue;
    public double MaxDeliveryTimeMs { get; set; }
    public double TotalDeliveryTimeMs { get; set; }
    
    // Distance stats
    public double TotalDistanceTraveled { get; set; }
    public double AverageHopDistance => TotalHops > 0 ? TotalDistanceTraveled / TotalHops : 0;
    
    // Time
    public TimeSpan SimulationDuration { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    // Throughput
    public double PacketsPerSecond => SimulationDuration.TotalSeconds > 0 
        ? TotalPacketsDelivered / SimulationDuration.TotalSeconds 
        : 0;
    
    // Per-device stats
    public Dictionary<Guid, DeviceStatistics> DeviceStats { get; set; } = [];
    public Dictionary<Guid, double> BatteryDrainByDevice { get; set; } = [];
    
    // Logs
    public List<PacketLogEntry> PacketLog { get; set; } = [];
    public List<HopLogEntry> HopLog { get; set; } = [];
    public List<LossLogEntry> LossLog { get; set; } = [];
    
    /// <summary>
    /// —оздать глубокую копию статистики
    /// </summary>
    public SimulationStatistics Clone()
    {
        return new SimulationStatistics
        {
            TotalPacketsCreated = TotalPacketsCreated,
            TotalPacketsDelivered = TotalPacketsDelivered,
            TotalPacketsDropped = TotalPacketsDropped,
            TotalPacketsForwarded = TotalPacketsForwarded,
            TotalPacketsLost = TotalPacketsLost,
            TotalHops = TotalHops,
            AverageDeliveryTimeMs = AverageDeliveryTimeMs,
            MinDeliveryTimeMs = MinDeliveryTimeMs,
            MaxDeliveryTimeMs = MaxDeliveryTimeMs,
            TotalDeliveryTimeMs = TotalDeliveryTimeMs,
            TotalDistanceTraveled = TotalDistanceTraveled,
            SimulationDuration = SimulationDuration,
            StartTime = StartTime,
            EndTime = EndTime,
            DeviceStats = DeviceStats.ToDictionary(kv => kv.Key, kv => new DeviceStatistics
            {
                DeviceId = kv.Value.DeviceId,
                DeviceName = kv.Value.DeviceName,
                PacketsSent = kv.Value.PacketsSent,
                PacketsReceived = kv.Value.PacketsReceived,
                PacketsForwarded = kv.Value.PacketsForwarded,
                PacketsDropped = kv.Value.PacketsDropped,
                TotalBatteryDrain = kv.Value.TotalBatteryDrain
            }),
            BatteryDrainByDevice = new Dictionary<Guid, double>(BatteryDrainByDevice),
            PacketLog = PacketLog.ToList(),
            HopLog = HopLog.ToList(),
            LossLog = LossLog.ToList()
        };
    }
}

public class DeviceStatistics
{
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = "";
    public int PacketsSent { get; set; }
    public int PacketsReceived { get; set; }
    public int PacketsForwarded { get; set; }
    public int PacketsDropped { get; set; }
    public double TotalBatteryDrain { get; set; }
}

public class PacketLogEntry
{
    public Guid PacketId { get; set; }
    public Guid IdempotencyId { get; set; }
    public string PacketType { get; set; } = "";
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public Guid? ReceiverId { get; set; }
    public string ReceiverName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public double? DeliveryTimeMs { get; set; }
    public string Status { get; set; } = "Created";
    public int HopCount { get; set; }
    public double TotalDistance { get; set; }
    public string? DropReason { get; set; }
    public List<Guid> Path { get; set; } = [];
}

public class HopLogEntry
{
    public DateTime Timestamp { get; set; }
    public Guid PacketId { get; set; }
    public Guid FromDeviceId { get; set; }
    public Guid ToDeviceId { get; set; }
    public float Distance { get; set; }
    public int HopNumber { get; set; }
}

public class LossLogEntry
{
    public DateTime Timestamp { get; set; }
    public Guid PacketId { get; set; }
    public float Distance { get; set; }
    public double LossChance { get; set; }
    public Guid FromDeviceId { get; set; }
    public Guid ToDeviceId { get; set; }
}
