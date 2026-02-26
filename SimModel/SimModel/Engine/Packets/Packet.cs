using Engine.Devices;

namespace Engine.Packets;

/// <summary>
/// Represents a unit of data travelling through the simulation network.
/// A packet knows its originator (<see cref="From"/>), its intended
/// destination (<see cref="To"/>), and the <see cref="NextHop"/> device it
/// will be delivered to on the next simulation tick.
/// Travel time is expressed in engine ticks via <see cref="TicksToTravel"/>,
/// and each hop decrements the <see cref="TTL"/> counter — a packet that
/// reaches zero TTL is silently dropped.
/// </summary>
public class Packet(Device from, Device to, PacketData payload)
{
    /// <summary>Gets the unique identifier of this packet instance.</summary>
    public Guid Id { get; } = Guid.NewGuid();

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
    /// Gets or sets the time-to-live counter. Decremented on every hop;
    /// the packet is dropped when it reaches zero.
    /// </summary>
    public int TTL { get; set; } = 10;

    /// <summary>Gets the device that originally sent this packet.</summary>
    public Device From { get; } = from;

    /// <summary>Gets the intended destination device for this packet.</summary>
    public Device To { get; } = to;

    /// <summary>Gets the data payload carried by this packet.</summary>
    public PacketData Payload { get; } = payload;

    /// <summary>
    /// Gets or sets a value indicating whether the destination device should
    /// send a <see cref="ConfirmationPacket"/> back to <see cref="From"/>
    /// upon successful delivery.
    /// </summary>
    public bool NeedConfirmation { get; set; } = false;

    /// <summary>
    /// Creates a shallow copy of this packet, suitable for broadcasting the
    /// same packet to multiple next-hop neighbours.
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
        {
            return Id.Equals(p.Id);
        }

        return base.Equals(obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}