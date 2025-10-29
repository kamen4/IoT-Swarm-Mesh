using System.Numerics;
using System.Text;
using Core.Contracts;

namespace Core;

public class Device : ICloneable
{
    public enum Type
    {
        Hub,
        Lamp,
        Sensor,
    }

    public enum PowerType
    {
        Battery,
        AC,
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Type DeviceType { get; set; } = Type.Sensor;
    public string Name { get; set; } = "";
    public double Battery { get; set; } = 1;
    public PowerType DevicePowerType { get; set; } = PowerType.Battery;
    public double Radius { get; set; } = 50;

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

    public Vector2 Pos { get; set; } = new();
    public string Color => DeviceType switch
        {
            Type.Hub => "#7337bd",
            Type.Lamp => "#c2ae1b",
            Type.Sensor => "#299450",
            _ => "#000000"
        };
    public int SizeR => DeviceType switch
    {
        Type.Hub => 20,
        Type.Lamp => 15,
        Type.Sensor => 10,
        _ => 0
    };

    public IPacketHandler? PacketHandler { get; set; } = new BroadcastBacktrackPH();
    
    public void HandlePacket(Packet packet)
    {
        if (PacketHandler is null)
        {
            throw new NullReferenceException(nameof(PacketHandler));
        }
        PacketHandler.Handle(this, packet);
    }

    public void AcceptPacket(Packet packet)
    {
        Console.WriteLine($"Msg accepted by {Id}:\n{Encoding.UTF8.GetString(packet.Payload ?? [])}");
    }

    public object Clone()
    {
        var mclone = (Device)MemberwiseClone();
        mclone.Id = Guid.NewGuid();
        return mclone;
    }
}
