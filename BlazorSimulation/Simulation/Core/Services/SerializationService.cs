using Core.Devices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Services;

public class SerializationService : ISerializationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Dictionary<string, string> _savedPresets = [];

    public string SerializeDevices(IEnumerable<Device> devices)
    {
        var dtos = devices.Select(DeviceToDto).ToList();
        return JsonSerializer.Serialize(dtos, JsonOptions);
    }

    public List<Device> DeserializeDevices(string data)
    {
        var dtos = JsonSerializer.Deserialize<List<DeviceDto>>(data, JsonOptions);
        return dtos?.Select(DtoToDevice).ToList() ?? [];
    }

    public string SerializeState(SimulationSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public SimulationSnapshot DeserializeState(string data)
    {
        return JsonSerializer.Deserialize<SimulationSnapshot>(data, JsonOptions) 
            ?? new SimulationSnapshot();
    }

    public void SavePreset(string name, IEnumerable<Device> devices)
    {
        _savedPresets[name] = SerializeDevices(devices);
    }

    public List<Device>? LoadPreset(string name)
    {
        return _savedPresets.TryGetValue(name, out var data) 
            ? DeserializeDevices(data) 
            : null;
    }

    public IEnumerable<string> GetSavedPresets() => _savedPresets.Keys;

    private static DeviceDto DeviceToDto(Device device)
    {
        var dto = new DeviceDto
        {
            Id = device.Id,
            Name = device.Name,
            Type = device.GetType().Name,
            X = device.Pos.X,
            Y = device.Pos.Y,
            Radius = device.Radius,
            Battery = device.Battery,
            PowerType = device.DevicePowerType.ToString(),
            BatteryDrainRate = device.BatteryDrainRate
        };

        if (device is Lamp lamp)
        {
            dto.IsLampOn = lamp.IsLampOn;
        }

        return dto;
    }

    private static Device DtoToDevice(DeviceDto dto)
    {
        Device device = dto.Type switch
        {
            nameof(Hub) => new Hub(dto.Id),
            nameof(Lamp) => new Lamp(dto.Id) { IsLampOn = dto.IsLampOn ?? true },
            nameof(Sensor) => new Sensor(dto.Id),
            _ => throw new InvalidOperationException($"Unknown device type: {dto.Type}")
        };

        device.Name = dto.Name;
        device.Pos = new(dto.X, dto.Y);
        device.Radius = dto.Radius;
        device.Battery = dto.Battery;
        device.BatteryDrainRate = dto.BatteryDrainRate;
        
        if (Enum.TryParse<Device.PowerType>(dto.PowerType, out var powerType))
        {
            device.DevicePowerType = powerType;
        }

        return device;
    }
}
