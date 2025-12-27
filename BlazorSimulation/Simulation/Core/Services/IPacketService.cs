using Core.Devices;
using System.Numerics;

namespace Core.Services;

/// <summary>
/// Сервис управления пакетами
/// </summary>
public interface IPacketService
{
    IReadOnlyCollection<Packet> ActivePackets { get; }
    
    Packet CreatePacket(Device sender, Device? receiver, PacketType type, byte[]? payload = null);
    Packet CreatePing(Device sender, Device receiver);
    Packet CreateLampCommand(Device sender, Lamp lamp, bool turnOn);
    Packet CreateSensorData(Sensor sensor, Device receiver, double value);
    Packet CreateNetworkBuild(Device sender, Guid targetDeviceId);
    
    void RegisterPacket(Packet packet);
    void TerminatePacket(Packet packet);
    
    /// <summary>
    /// Устанавливает конфигурацию симуляции (для packet loss и скорости)
    /// </summary>
    void SetConfig(SimulationConfig config);
    
    /// <summary>
    /// Устанавливает обработчик для NetworkBuild пакетов
    /// </summary>
    void SetNetworkBuildHandler(IPacketHandler handler);
    
    /// <summary>
    /// Обрабатывает тик - перемещает пакеты и возвращает их позиции для отрисовки
    /// </summary>
    List<Vector2> Tick();
    
    void Clear();
    
    // Обработчики
    IReadOnlyDictionary<string, IPacketHandler> Handlers { get; }
    IPacketHandler ActiveHandler { get; }
    void SetActiveHandler(string name);
    IEnumerable<string> GetHandlerNames();
}

public enum PacketType
{
    Ping,
    Ack,
    Data,
    NetworkBuild,
    LampCommand,
    SensorData
}
