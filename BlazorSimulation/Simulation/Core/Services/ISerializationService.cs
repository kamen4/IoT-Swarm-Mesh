using Core.Devices;

namespace Core.Services;

/// <summary>
/// Сервис сериализации состояния симуляции
/// </summary>
public interface ISerializationService
{
    // Сериализация устройств
    string SerializeDevices(IEnumerable<Device> devices);
    List<Device> DeserializeDevices(string data);
    
    // Сериализация состояния (включая связи)
    string SerializeState(SimulationSnapshot snapshot);
    SimulationSnapshot DeserializeState(string data);
    
    // Пресеты
    void SavePreset(string name, IEnumerable<Device> devices);
    List<Device>? LoadPreset(string name);
    IEnumerable<string> GetSavedPresets();
}

public class SimulationSnapshot
{
    public List<DeviceDto> Devices { get; set; } = [];
    public List<ConnectionDto> Connections { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
}

public class DeviceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public double Radius { get; set; }
    public double Battery { get; set; }
    public string PowerType { get; set; } = "Battery";
    public double BatteryDrainRate { get; set; }
    
    // Lamp specific
    public bool? IsLampOn { get; set; }
}

public class ConnectionDto
{
    public Guid DeviceId1 { get; set; }
    public Guid DeviceId2 { get; set; }
}
