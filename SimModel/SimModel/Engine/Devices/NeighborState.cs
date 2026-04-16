using Engine.Packets;

namespace Engine.Devices;

/// <summary>
/// Tracks protocol neighbor metadata used by charge-based routing and
/// parent selection.
/// </summary>
public sealed class NeighborState
{
    /// <summary>
    /// Initializes a neighbor entry.
    /// </summary>
    /// <param name="macAddress">Neighbor MAC address.</param>
    public NeighborState(byte[] macAddress)
    {
        MacAddress = PacketAddress.Clone(macAddress);
    }

    /// <summary>
    /// Gets the neighbor MAC address.
    /// </summary>
    public byte[] MacAddress { get; }

    /// <summary>
    /// Gets or sets the best observed UP charge.
    /// </summary>
    public ushort QUp { get; set; }

    /// <summary>
    /// Gets or sets the best observed TOTAL charge.
    /// </summary>
    public ushort QTotal { get; set; }

    /// <summary>
    /// Gets or sets the engine tick when this neighbor was last observed.
    /// </summary>
    public long LastSeenTick { get; set; }

    /// <summary>
    /// Gets or sets the number of packets observed from this neighbor.
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// Gets or sets the learned effective link quality in range [0, 1].
    /// </summary>
    public double LinkQuality { get; set; } = 0.0;
}
