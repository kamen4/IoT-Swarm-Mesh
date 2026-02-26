using Engine.Devices;

namespace Engine.Packets;

public class Packet(Device from, Device to, PacketData payload)
{
    public Guid Id { get; } = Guid.NewGuid();

    public long TicksToTravel { get; set; } = 3;
    public long ArrivalTick { get; set; } = 0;
    public Device NextHop { get; set; } = null!;
    public int TTL { get; set; } = 10;

    public Device From { get; } = from;
    public Device To { get; } = to;
    public PacketData Payload { get; } = payload;

    public bool NeedConfirmation { get; set; } = false;

    public Packet Clone()
    {
        return (MemberwiseClone() as Packet)!;
    }

    public override bool Equals(object? obj)
    {
        if (obj is Packet p)
        {
            return Id.Equals(p.Id);
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}