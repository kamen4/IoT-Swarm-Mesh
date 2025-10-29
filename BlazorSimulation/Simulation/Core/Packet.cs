using System.Security.Cryptography;
using Core.Managers;

namespace Core;

public class Packet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Device Sender { get; set; }
    public Device Receiver { get; set; }
    public byte[]? Payload { get; set; }
    public bool ConfirmDelivery { get; set; } = false; 
    public bool DirectionForward { get; set; } = true;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public Device? CurrentHop { get; set; }
    public Device? NextHop { get; set; }

    public object? HandlerData { get; set; }

    public List<Device>? ConnectedDevices { get; set; }

    public Packet() 
    {
        PacketManager.RegisterPacket(this);
    }

    public Packet(Device sender, Device reciever)
    {
        Sender = sender;
        CurrentHop = sender;
        NextHop = sender;
        Receiver = reciever;
        PacketManager.RegisterPacket(this);
    }

    public Packet RemakeForNextHop(Device hop)
    {
        return new Packet()
        {
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
