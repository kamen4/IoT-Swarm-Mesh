using System.Numerics;
using System.Text;
using Core.Managers;

namespace Core.Devices;

public abstract class Device : ICloneable
{
    public enum PowerType
    {
        Battery,
        AC,
    }

    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public double Battery { get; set; } = 1;
    public PowerType DevicePowerType { get; set; } = PowerType.Battery;
    public double Radius { get; set; } = 50;

    public Device()
    {
        Id = Guid.NewGuid();
    }

    public Device(Guid id)
    {
        Id = id;   
    }

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

    public abstract string Color { get; }
    public abstract int SizeR { get; }

    public void HandlePacket(Packet packet)
    {
        HandlerManager.GetActiveHandler().Handle(this, packet);
    }

    private static readonly HashSet<Guid> _idempKeys = [];
    public virtual void AcceptPacket(Packet packet)
    {
        if (!_idempKeys.Contains(packet.IdempotencyId))
        {
            _idempKeys.Add(packet.IdempotencyId);
            Console.WriteLine($"Msg accepted by {Id}: {Encoding.UTF8.GetString(packet.Payload ?? [])}");

            switch (packet.PacketType)
            {
                case Packet.Type.Ping:
                    break;
                case Packet.Type.FindDevice:
                    break;
                default:
                    break;
            }

            if (packet.ConfirmDelivery && packet.DirectionForward)
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

    public virtual object Clone()
    {
        var mclone = (Device)MemberwiseClone();
        mclone.Id = Guid.NewGuid();
        return mclone;
    }
}