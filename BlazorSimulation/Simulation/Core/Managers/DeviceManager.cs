using System.Numerics;

namespace Core.Managers;

public static class DeviceManager
{
    public static HashSet<Device> Devices { get; } = 
        [
            new Device() { Name = "HUB", Pos = new(100,100), Radius = 300, DeviceType = Device.Type.Hub}
        ];

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
