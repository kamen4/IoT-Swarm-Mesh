using Common.Entities;

namespace BusinessServer.Services;

/// <summary>Defines the contract for an in-memory registry that stores and retrieves IoT device metadata.</summary>
public interface IDeviceRegistryService
{
    /// <summary>Returns all devices currently held in the registry.</summary>
    IEnumerable<DeviceInfo> GetAll();

    /// <summary>
    /// Returns the device with the specified ID, or <see langword="null"/> if no matching device is registered.
    /// </summary>
    /// <param name="deviceId">Unique device identifier to search for.</param>
    DeviceInfo? GetById(string deviceId);

    /// <summary>
    /// Adds a new device to the registry or replaces an existing entry that shares the same DeviceId.
    /// </summary>
    /// <param name="device">Device metadata to store.</param>
    /// <returns>The stored device record (same object that was passed in).</returns>
    DeviceInfo Register(DeviceInfo device);
}
