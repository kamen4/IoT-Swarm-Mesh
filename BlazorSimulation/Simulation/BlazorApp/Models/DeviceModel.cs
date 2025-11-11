using Core.Devices;
using static Core.Devices.Device;

namespace BlazorApp.Models;

public class DeviceModel
{
    public enum Type
    {
        Hub,
        Sensor,
        Lamp
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public double Battery { get; set; } = 1;
    public PowerType DevicePowerType { get; set; } = PowerType.Battery;
    public double Radius { get; set; } = 50;
    public Type DeviceType { get; set; } = Type.Sensor;

    public Device ToDevice()
    {
        Device device = DeviceType switch
        {
            Type.Hub => new Hub(Id),
            Type.Lamp => new Lamp(Id),
            Type.Sensor => new Sensor(Id),
            _ => throw new NotImplementedException()
        };
        ApplyTo(device);
        return device;
    }

    public void ApplyTo(Device? device)
    {
        if (device is null)
        {
            return;
        }
        device.Name = Name;
        device.Battery = Battery;
        device.DevicePowerType = DevicePowerType;
        device.Radius = Radius;
    }

    public DeviceModel()
    {
    }

    public DeviceModel(Device device)
    {
        Id = device.Id;
        Name = device.Name;
        Battery = device.Battery;
        DevicePowerType = device.DevicePowerType;
        Radius = device.Radius;
    }
}
