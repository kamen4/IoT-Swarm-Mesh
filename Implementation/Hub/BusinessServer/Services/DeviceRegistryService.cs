using System.Collections.Concurrent;
using Common.Entities;

namespace BusinessServer.Services;

public class DeviceRegistryService : IDeviceRegistryService
{
    private readonly ConcurrentDictionary<string, DeviceInfo> _devices = new();

    public IEnumerable<DeviceInfo> GetAll() => _devices.Values;

    public DeviceInfo? GetById(string deviceId)
        => _devices.TryGetValue(deviceId, out var device) ? device : null;

    public DeviceInfo Register(DeviceInfo device)
    {
        _devices[device.DeviceId] = device;
        return device;
    }
}
