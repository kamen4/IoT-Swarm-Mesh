using System.Collections.Concurrent;
using Common.Entities;

namespace BusinessServer.Services;

/// <summary>Thread-safe in-memory implementation of <see cref="IDeviceRegistryService"/> backed by a concurrent dictionary.</summary>
public class DeviceRegistryService : IDeviceRegistryService
{
    private readonly ConcurrentDictionary<string, DeviceInfo> _devices = new();

    /// <inheritdoc/>
    public IEnumerable<DeviceInfo> GetAll() => _devices.Values;

    /// <inheritdoc/>
    public DeviceInfo? GetById(string deviceId)
        => _devices.TryGetValue(deviceId, out var device) ? device : null;

    /// <inheritdoc/>
    public DeviceInfo Register(DeviceInfo device)
    {
        _devices[device.DeviceId] = device;
        return device;
    }
}
