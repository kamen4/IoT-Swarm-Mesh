using Engine.Core;
using Engine.Packets;
using Engine.Routers;
using System.Numerics;

namespace Engine.Devices;

public abstract class Device
{
    public Guid Id { get; } = Guid.NewGuid();
    public Vector2 Position { get; set; }
    public void Recieve(Packet packet)
    {
        if (!packet.To.Equals(this))
        {
            PacketRouter.Instance.Route(packet, this);
            return;
        }

        if (packet.NeedConfirmation)
        {
            var confirmationPacket = new ConfirmationPacket(packet);
            PacketRouter.Instance.Route(confirmationPacket, this);
        }
        Accept(packet);
    }

    public abstract void Accept(Packet packet);
}