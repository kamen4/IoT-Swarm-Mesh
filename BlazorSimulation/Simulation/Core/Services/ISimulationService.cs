using Core.Devices;
using Core.Statistics;

namespace Core.Services;

/// <summary>
/// Главный сервис симуляции - точка входа для всех операций
/// </summary>
public interface ISimulationService
{
    // Управление устройствами
    IDeviceService Devices { get; }
    
    // Управление пакетами
    IPacketService Packets { get; }
    
    // Построение сети
    INetworkBuilderService NetworkBuilder { get; }
    
    // Статистика
    IStatisticsService Statistics { get; }
    
    // Сериализация
    ISerializationService Serialization { get; }

    // Состояние симуляции
    SimulationState State { get; }
    
    // Управление симуляцией
    Task StartNetworkBuildAsync();
    Task StartDataSimulationAsync(SimulationConfig config);
    void StopSimulation();
    void Reset();
    
    // Тик симуляции (для анимации)
    SimulationTickResult Tick();
    
    // События
    event Action<SimulationState>? OnStateChanged;
    event Action<PacketEventArgs>? OnPacketEvent;
}

public enum SimulationState
{
    Idle,
    BuildingNetwork,
    Running,
    Paused,
    Completed
}

public class SimulationConfig
{
    public string ProtocolName { get; set; } = "";
    public string NetworkStrategy { get; set; } = "";
    public int PacketIntervalMs { get; set; } = 500;
    public int SimulationDurationMs { get; set; } = 30000;
    public double BatteryDrainPerPacket { get; set; } = 0.01;
    public double BatteryDrainPerSecond { get; set; } = 0.001;
    public bool RandomLampCommands { get; set; } = true;
    public bool RandomSensorData { get; set; } = true;
    
    /// <summary>
    /// Базовый шанс потери пакета (0-1)
    /// </summary>
    public double BasePacketLossRate { get; set; } = 0.0;
    
    /// <summary>
    /// Дополнительный шанс потери на единицу расстояния
    /// </summary>
    public double DistancePacketLossRate { get; set; } = 0.0;
    
    /// <summary>
    /// Скорость пакетов (пикселей в секунду)
    /// </summary>
    public float PacketSpeed { get; set; } = 200f;
    
    /// <summary>
    /// Создать копию конфига
    /// </summary>
    public SimulationConfig Clone()
    {
        return new SimulationConfig
        {
            ProtocolName = ProtocolName,
            NetworkStrategy = NetworkStrategy,
            PacketIntervalMs = PacketIntervalMs,
            SimulationDurationMs = SimulationDurationMs,
            BatteryDrainPerPacket = BatteryDrainPerPacket,
            BatteryDrainPerSecond = BatteryDrainPerSecond,
            RandomLampCommands = RandomLampCommands,
            RandomSensorData = RandomSensorData,
            BasePacketLossRate = BasePacketLossRate,
            DistancePacketLossRate = DistancePacketLossRate,
            PacketSpeed = PacketSpeed
        };
    }
}

public class SimulationTickResult
{
    public List<System.Numerics.Vector2> PacketPositions { get; set; } = [];
    public bool HasActivePackets { get; set; }
}

public class PacketEventArgs
{
    public required Packet Packet { get; init; }
    public PacketEventType EventType { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public enum PacketEventType
{
    Created,
    Delivered,
    Dropped,
    Forwarded,
    Lost
}
