using Engine.Core;
using Engine.Devices;
using Engine.Packets;

namespace Engine.Routers;

/// <summary>
/// Implements a flooding broadcast routing strategy.
/// <para>
/// When asked to route a packet the router queries the supplied
/// <see cref="INetworkTopology"/> for all devices <em>visible</em> to the
/// sender and enqueues a cloned copy addressed to each of them.
/// Flooding uses <em>visibility</em> — not established connections — because
/// it is inherently a broadcast mechanism that needs no prior topology
/// knowledge.
/// </para>
/// <para>
/// This class is no longer a singleton. The active router instance is stored
/// on <see cref="SimulationEngine.Router"/> and is injected into every routing
/// call automatically by <see cref="Device.Recieve"/>.
/// To change the routing strategy at runtime replace
/// <see cref="SimulationEngine.Router"/> with a different
/// <see cref="IPacketRouter"/> implementation.
/// </para>
/// </summary>
public class FloodingPacketRouter : IPacketRouter
{
    /// <inheritdoc/>
    public string Name => "Flooding Broadcast";

    /// <inheritdoc/>
    /// <remarks>
    /// Broadcasts <paramref name="packet"/> to every device that is within
    /// <see cref="SimulationEngine.VisibilityDistance"/> of <paramref name="sender"/>.
    /// Each visible neighbour receives its own shallow clone so that per-hop
    /// fields (<see cref="Packet.NextHop"/>, <see cref="Packet.ArrivalTick"/>)
    /// can be set independently.
    /// </remarks>
    public void Route(Packet packet, Device sender, INetworkTopology topology)
    {
        foreach (var d in topology.GetVisibleDevices(sender))
        {
            var p = packet.Clone();
            p.NextHop = d;
            SimulationEngine.Instance.RegisterPacket(p);
        }
    }
}