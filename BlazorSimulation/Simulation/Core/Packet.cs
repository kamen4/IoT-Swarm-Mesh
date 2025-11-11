using Core.Devices;
using Core.Managers;

namespace Core;

public class Packet
{
    public enum Type
    {
        Ping,
        Data,
        FindDevice,
    };

    public Guid IdempotencyId { get; set; } = Guid.NewGuid();
    public Type PacketType { get; set; } = Type.Ping;
    public Device Sender { get; set; } = null!;
    public Device Receiver { get; set; } = null!;
    public byte[]? Payload { get; set; }
    public bool ConfirmDelivery { get; set; } = true;
    public bool DirectionForward { get; set; } = true;

    public Guid Id { get; } = Guid.NewGuid();
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public Device? CurrentHop { get; set; }
    public Device? NextHop { get; set; }

    public object? HandlerData { get; set; }

    public List<Device>? ConnectedDevices { get; set; }

    public Packet()
    {
        PacketManager.RegisterPacket(this);
    }

    public Packet(Device sender, Device receiver)
    {
        Sender = sender;
        CurrentHop = sender;
        NextHop = sender;
        Receiver = receiver;
        PacketManager.RegisterPacket(this);
    }

    public Packet RemakeForNextHop(Device hop)
    {
        return new Packet()
        {
            IdempotencyId = IdempotencyId,
            Sender = Sender,
            Receiver = Receiver,
            Payload = Payload,
            ConfirmDelivery = ConfirmDelivery,
            DirectionForward = DirectionForward,
            CurrentHop = NextHop,
            NextHop = hop
        };
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    internal void Terminate()
    {
        PacketManager.ActivePackets.Remove(this);
    }
}