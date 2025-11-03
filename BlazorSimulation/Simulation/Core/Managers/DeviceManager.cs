using System.Numerics;

namespace Core.Managers;

public static class DeviceManager
{
    public static HashSet<Device> Devices { get; } = 
        [
            new Device() { Name = "HUB", Pos = new(100,100), Radius = 300, DeviceType = Device.Type.Hub}
        ];

    public static void SetPredefinedPreset(string presetName)
    {
        switch (presetName)
        {
            case "Diamonds":
                {
                    Devices.Clear();
                    Devices.EnsureCapacity(10);
                    Devices.Add(new Device() { Name = "H", Pos = new(50, 50), Radius = 200, DeviceType = Device.Type.Hub });

                    Devices.Add(new Device() { Name = "d1", Pos = new(50, 250), Radius = 300, DeviceType = Device.Type.Lamp });
                    Devices.Add(new Device() { Name = "d2", Pos = new(50, 450), Radius = 300, DeviceType = Device.Type.Sensor });
                    Devices.Add(new Device() { Name = "d3", Pos = new(250, 450), Radius = 300, DeviceType = Device.Type.Lamp });
                    Devices.Add(new Device() { Name = "d4", Pos = new(250, 250), Radius = 300, DeviceType = Device.Type.Sensor });

                    Devices.Add(new Device() { Name = "d5", Pos = new(550, 450), Radius = 300, DeviceType = Device.Type.Sensor });
                    Devices.Add(new Device() { Name = "d6", Pos = new(750, 450), Radius = 300, DeviceType = Device.Type.Lamp });
                    Devices.Add(new Device() { Name = "d7", Pos = new(550, 650), Radius = 300, DeviceType = Device.Type.Lamp });
                    Devices.Add(new Device() { Name = "d8", Pos = new(750, 650), Radius = 300, DeviceType = Device.Type.Sensor });

                    Devices.Add(new Device() { Name = "d9", Pos = new(1050, 650), Radius = 300, DeviceType = Device.Type.Sensor });
                    break;
                }
        }
    }

#warning TODO BETTER ALGORITHM
    public static IEnumerable<(Device d1, Device d2)> GetAllVisibilities()
    {
        foreach (var d1 in Devices)
        {
            foreach (var d2 in Devices)
            {
                if (d1 != d2 && AreDevicesVisible(d1, d2))
                {
                    yield return (d1, d2);
                }
            }
        }
    }

    public static IEnumerable<Device> GetVisibilitiesForDevice(Device device)
    {
        foreach (var d in Devices)
        {
            if (d != device && AreDevicesVisible(d, device))
            {
                yield return d;
            }
        }
    }

    private static bool AreDevicesVisible(Device d1, Device d2)
    {
        var l2 = (d1.Pos - d2.Pos).LengthSquared();
        var rmin = Math.Min(d1.Radius, d2.Radius);
        return l2 <= rmin * rmin;
    }
}
