using Engine.Devices;

namespace Engine.Packets;

/// <summary>
/// Represents a unit of data travelling through the simulation network.
/// A packet knows its originator (<see cref="From"/>), its intended
/// destination (<see cref="To"/>), and the <see cref="NextHop"/> device it
/// will be delivered to on the next simulation tick.
/// Travel time is expressed in engine ticks via <see cref="TicksToTravel"/>,
/// and each hop decrements the <see cref="TTL"/> counter  -  a packet that
/// reaches zero TTL is silently dropped.
/// </summary>
public class Packet
{
    /// <summary>
    /// Initialises a new packet.
    /// <see cref="OriginId"/> is set to the same value as <see cref="Id"/> so
    /// that all flood clones produced by <see cref="Clone"/> inherit the
    /// original packet's identity.
    /// </summary>
    public Packet(Device from, Device to, PacketData payload)
    {
        From     = from;
        To       = to;
        Payload  = payload;
        Id       = Guid.NewGuid();
        OriginId = Id;   // clones will keep this value via MemberwiseClone
    }

    /// <summary>Gets the unique identifier of this packet instance.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the identifier shared by this packet and all of its flood clones.
    /// On the original packet <c>OriginId == Id</c>.
    /// After <see cref="Clone"/>, every copy keeps the same <see cref="OriginId"/>
    /// because <c>MemberwiseClone</c> copies the field as-is.
    /// <para>
    /// <see cref="Statistics.SimulationStatistics"/> uses this to detect duplicate
    /// deliveries: when a second clone with the same <see cref="OriginId"/> reaches
    /// the destination it is counted as a duplicate rather than a new delivery.
    /// </para>
    /// </summary>
    public Guid OriginId { get; }

    /// <summary>
    /// Gets the TTL value that was recorded when this packet was first enqueued
    /// by <see cref="Engine.Core.SimulationEngine.RegisterPacket"/>.
    /// <para>
    /// Set once on the first enqueue; clones inherit it through
    /// <c>MemberwiseClone</c> so every clone knows the starting TTL.
    /// Used by <see cref="Statistics.SimulationStatistics"/> to compute
    /// hop count at delivery: <c>hops = InitialTtl - TTL</c>.
    /// </para>
    /// </summary>
    public int InitialTtl { get; internal set; }

    /// <summary>
    /// Gets or sets the number of engine ticks the packet takes to travel
    /// between two adjacent devices.
    /// </summary>
    public long TicksToTravel { get; set; } = 3;

    /// <summary>
    /// Gets or sets the absolute engine tick at which this packet is due to
    /// arrive at <see cref="NextHop"/>.
    /// Set by <see cref="Engine.Core.SimulationEngine.RegisterPacket"/> when the packet
    /// is enqueued.
    /// </summary>
    public long ArrivalTick { get; set; } = 0;

    /// <summary>Gets or sets the device that will receive this packet on arrival.</summary>
    public Device NextHop { get; set; } = null!;

    /// <summary>
    /// Gets or sets the device that forwarded this packet to <see cref="NextHop"/>
    /// on the previous hop.
    /// <para>
    /// Set by <see cref="Engine.Devices.Device.Recieve"/> just before the packet is
    /// re-routed, so that the active <see cref="Engine.Routers.IPacketRouter"/> can
    /// skip sending the packet back to the node it just came from.
    /// <c>null</c> on the first hop (packet originates directly from <see cref="From"/>).
    /// </para>
    /// </summary>
    public Device? PreviousHop { get; set; } = null;

    /// <summary>
    /// Gets or sets the time-to-live counter. Decremented on every hop;
    /// the packet is dropped when it reaches zero.
    /// </summary>
    public int TTL { get; set; } = 10;

    /// <summary>Gets the device that originally sent this packet.</summary>
    public Device From { get; }

    /// <summary>Gets the intended destination device for this packet.</summary>
    public Device To { get; }

    /// <summary>Gets the data payload carried by this packet.</summary>
    public PacketData Payload { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the destination device should
    /// send a <see cref="ConfirmationPacket"/> back to <see cref="From"/>
    /// upon successful delivery.
    /// </summary>
    public bool NeedConfirmation { get; set; } = false;

    /// <summary>
    /// Creates a shallow copy of this packet, suitable for broadcasting the
    /// same packet to multiple next-hop neighbours.
    /// All clones share the same <see cref="OriginId"/> as the original.
    /// </summary>
    /// <returns>A shallow clone of this packet.</returns>
    public Packet Clone()
    {
        return (MemberwiseClone() as Packet)!;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is Packet p)
            return Id.Equals(p.Id);
        return base.Equals(obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();
}