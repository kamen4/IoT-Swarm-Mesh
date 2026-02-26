using Engine.Core;
using Engine.Packets;
using Engine.Routers;
using System.Numerics;

namespace Engine.Devices;

/// <summary>
/// Abstract base class for all devices in the simulation network.
/// A device has a unique identity and a position in 2D space.
/// When it receives a packet it either forwards it (if it is not the
/// intended recipient) or accepts and processes it.
/// </summary>
public abstract class Device
{
    /// <summary>Gets or sets the human-readable name of this device.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets the unique identifier of this device.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Gets or sets the 2D position of this device in the simulation world.</summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Called when a packet arrives at this device.
    /// If the packet is addressed to another device the router broadcasts it
    /// to all visible neighbours. If the packet is addressed to this device
    /// and delivery confirmation was requested, a <see cref="ConfirmationPacket"/>
    /// is sent back to the originator before <see cref="Accept"/> is invoked.
    /// </summary>
    /// <param name="packet">The packet that has arrived at this device.</param>
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

    /// <summary>
    /// Processes a packet that is addressed to and has been delivered to this device.
    /// Derived classes implement the specific handling logic here.
    /// </summary>
    /// <param name="packet">The delivered packet to process.</param>
    public abstract void Accept(Packet packet);
}