using Engine.Core;
using Engine.Packets;
using System.Numerics;

namespace Engine.Devices;

/// <summary>
/// Abstract base class for all devices in the simulation network.
/// A device has a unique identity and a position in 2D space.
/// When it receives a packet it either forwards it (if it is not the
/// intended recipient) or accepts and processes it.
/// <para>
/// Forwarding is delegated to <see cref="SimulationEngine.RoutePacket"/> so that
/// the active <see cref="Engine.Routers.IPacketRouter"/> is always consulted.
/// Devices never reference a concrete router directly.
/// </para>
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
    /// If the packet is addressed to another device, routing is delegated to
    /// <see cref="SimulationEngine.RoutePacket"/> which uses the currently
    /// configured <see cref="Engine.Routers.IPacketRouter"/>.
    /// If the packet is addressed to this device and delivery confirmation was
    /// requested, a <see cref="ConfirmationPacket"/> is routed back to the
    /// originator before <see cref="Accept"/> is invoked.
    /// </summary>
    /// <param name="packet">The packet that has arrived at this device.</param>
    public void Recieve(Packet packet)
    {
        if (!packet.To.Equals(this))
        {
            SimulationEngine.Instance.RoutePacket(packet, this);
            return;
        }

        if (packet.NeedConfirmation)
        {
            var confirmationPacket = new ConfirmationPacket(packet);
            SimulationEngine.Instance.RoutePacket(confirmationPacket, this);
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