using System.Numerics;
using System.Text;
using Core.Contracts;
using Core.Managers;

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

    public List<Device> Connections { get; set; } = [];

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

    public void HandlePacket(Packet packet)
    {
        HandlerManager.GetActiveHandler().Handle(this, packet);
    }

    private static readonly HashSet<Guid> _idempKeys = [];
    public void AcceptPacket(Packet packet)
    {
        if (!_idempKeys.Contains(packet.IdempotencyId))
        {
            _idempKeys.Add(packet.IdempotencyId);
            Console.WriteLine($"Msg accepted by {Id}: {Encoding.UTF8.GetString(packet.Payload ?? [])}");

            if (packet.ConfirmDelivery &&  packet.DirectionForward)
            {
                _ = new Packet(this, packet.Sender)
                {
                    HandlerData = packet.HandlerData,
                    DirectionForward = false,
                    Payload = Encoding.UTF8.GetBytes($"MSG OK {packet.IdempotencyId}")
                };
            }
        }
    }

    public object Clone()
    {
        var mclone = (Device)MemberwiseClone();
        mclone.Id = Guid.NewGuid();
        return mclone;
    }
}
