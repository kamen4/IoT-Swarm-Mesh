using Engine.Devices;

namespace Engine.Packets;

/// <summary>
/// Broadcast gateway beacon used to seed and maintain DOWN-tree convergence.
/// </summary>
public sealed class BeaconPacket : Packet
{
    /// <summary>
    /// Default TTL for beacon dissemination.
    /// </summary>
    public const int DEFAULT_BEACON_TTL = 30;

    /// <summary>
    /// Initializes a beacon packet addressed to broadcast.
    /// </summary>
    /// <param name="hub">Hub that emits the beacon.</param>
    /// <param name="recommendedForwardThreshold">
    /// Suggested q_forward threshold for receivers.
    /// </param>
    public BeaconPacket(HubDevice hub, ushort recommendedForwardThreshold)
        : base(hub, hub, new PacketData
        {
            Data = new BeaconPayload(recommendedForwardThreshold),
        })
    {
        Direction = PacketDirection.Down;
        MessageType = SwarmMessageType.BEACON;
        DestinationMac = PacketAddress.Clone(PacketAddress.Broadcast);
        TTL = DEFAULT_BEACON_TTL;
    }
}
