using Engine.Devices;

namespace Engine.Packets;

/// <summary>
/// Broadcast packet carrying a monotonic decay epoch and damping factor.
/// </summary>
public sealed class DecayPacket : Packet
{
    /// <summary>
    /// Default TTL for decay dissemination.
    /// </summary>
    public const int DEFAULT_DECAY_TTL = 30;

    /// <summary>
    /// Initializes a decay packet addressed to broadcast.
    /// </summary>
    /// <param name="hub">Hub that emits the decay event.</param>
    /// <param name="decayEpoch">Monotonic epoch number.</param>
    /// <param name="decayPercent">Decay factor in range [0, 1].</param>
    public DecayPacket(HubDevice hub, ushort decayEpoch, double decayPercent)
        : base(hub, hub, new PacketData
        {
            Data = new DecayPayload(decayEpoch, decayPercent),
        })
    {
        Direction = PacketDirection.Down;
        MessageType = SwarmMessageType.DECAY;
        DestinationMac = PacketAddress.Clone(PacketAddress.Broadcast);
        DecayEpochHint = decayEpoch;
        TTL = DEFAULT_DECAY_TTL;
    }
}
