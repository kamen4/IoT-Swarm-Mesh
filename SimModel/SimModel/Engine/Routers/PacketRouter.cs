using Engine.Core;
using Engine.Devices;
using Engine.Packets;

namespace Engine.Routers;

/// <summary>
/// Implements a flooding broadcast routing strategy.
/// When asked to route a packet from a given device, the router queries
/// <see cref="SimulationEngine"/> for all devices visible to that device and
/// enqueues a cloned copy of the packet addressed to each of them.
/// This is the sole routing mechanism currently used by the engine.
/// </summary>
public class PacketRouter
{
    /// <summary>Gets the singleton instance of <see cref="PacketRouter"/>.</summary>
    public static PacketRouter Instance { get; } = new();

    /// <summary>
    /// Broadcasts <paramref name="packet"/> to every device that is within
    /// <see cref="SimulationEngine.VisibilityDistance"/> of <paramref name="device"/>.
    /// Each neighbour receives its own shallow clone so that per-hop fields
    /// (such as <see cref="Packet.NextHop"/> and <see cref="Packet.ArrivalTick"/>)
    /// can be set independently.
    /// </summary>
    /// <param name="packet">The packet to broadcast.</param>
    /// <param name="device">The device that is currently holding the packet.</param>
    public void Route(Packet packet, Device device)
    {
        foreach (var d in SimulationEngine.Instance.GetVisibleDevicesFor(device))
        {
            var p = packet.Clone();
            p.NextHop = d;
            SimulationEngine.Instance.RegisterPacket(p);
        }
    }
}