namespace Engine.Packets;

/// <summary>
/// Direction of a protocol frame in the swarm mesh.
/// </summary>
public enum PacketDirection : byte
{
    /// <summary>
    /// Device-to-gateway traffic.
    /// </summary>
    Up = 0,

    /// <summary>
    /// Gateway-to-device traffic.
    /// </summary>
    Down = 1,
}
