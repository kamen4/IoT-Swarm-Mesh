using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Core;

public class Device
{
    public enum Type
    {
        Hub = 30,
        Lamp = 20,
        Sensor = 15,
    }

    public enum PowerType
    {
        Battery,
        AC,
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Type DeviceType { get; set; } = Type.Sensor;
    public string Name { get; set; } = "";
    public double Battery { get; set; } = 1;
    public PowerType DevicePowerType { get; set; } = PowerType.Battery;
    public double Radius { get; set; } = 50;
    public Vector2 Pos { get; set; } = new();

    public override bool Equals(object? obj)
    {
        if (obj is Device d)
        {
            return Id.Equals(d.Id);
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public string Color => DeviceType switch
        {
            Type.Hub => "#7337bd",
            Type.Lamp => "#c2ae1b",
            Type.Sensor => "#299450",
            _ => "#000000"
        };
}
