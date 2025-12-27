using Core.Devices;
using System.Numerics;

namespace Core.Services;

/// <summary>
/// Сервис управления устройствами
/// </summary>
public interface IDeviceService
{
    IReadOnlyCollection<Device> All { get; }
    Hub? Hub { get; }
    IEnumerable<Lamp> Lamps { get; }
    IEnumerable<Sensor> Sensors { get; }

    void Add(Device device);
    void Remove(Device device);
    void Clear();
    
    Device? GetById(Guid id);
    Device? GetAtPosition(Vector2 pos, float tolerance = 15f);
    
    IEnumerable<Device> GetVisibleDevices(Device device);
    IEnumerable<(Device d1, Device d2)> GetAllVisibilities();
    bool AreDevicesVisible(Device d1, Device d2);
    
    void LoadPreset(string presetName);
    IEnumerable<string> GetPresetNames();
    
    event Action? OnDevicesChanged;
}
