using Common.Entities;

namespace BusinessServer.Services;

public interface IDeviceRegistryService
{
    IEnumerable<DeviceInfo> GetAll();
    DeviceInfo? GetById(string deviceId);
    DeviceInfo Register(DeviceInfo device);
}
