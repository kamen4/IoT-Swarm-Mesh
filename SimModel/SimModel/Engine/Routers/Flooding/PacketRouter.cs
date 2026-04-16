using Engine.Core;
using Engine.Devices;
using Engine.Packets;

namespace Engine.Routers;

/// <summary>
/// Implements a flooding broadcast routing strategy.
/// <para>
/// When asked to route a packet the router queries the supplied
/// <see cref="INetworkTopology"/> for all devices <em>visible</em> to the
/// sender and enqueues a cloned copy addressed to the nearest visible
/// neighbors in a deterministic top-k window.
/// Flooding uses <em>visibility</em>  -  not established connections  -  because
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
    /// <see cref="SimulationEngine.VisibilityDistance"/> of <paramref name="sender"/>
    /// but limits fan-out to
    /// <see cref="RoutingNeighborPolicy.DefaultMaxVisibleNeighbors"/> nearest
    /// visible neighbors.
    /// Each visible neighbour receives its own shallow clone so that per-hop
    /// fields (<see cref="Packet.NextHop"/>, <see cref="Packet.ArrivalTick"/>)
    /// can be set independently.
    /// </remarks>
    public void Route(Packet packet, Device sender, INetworkTopology topology)
    {
        var neighbors = RoutingNeighborPolicy.GetNearestVisibleNeighbors(sender, topology);

        foreach (var d in neighbors)
        {
            var p = packet.Clone();
            p.NextHop = d;
            p.PreviousHop = sender;
            p.PreviousHopMac = PacketAddress.Clone(sender.MacAddress);
            p.AdvertisedCharge = packet.Direction == PacketDirection.Up
                ? sender.QUpSelf
                : sender.QTotalSelf;
            p.DecayEpochHint = sender.LastDecayEpoch;
            SimulationEngine.Instance.RegisterPacket(p);
        }
    }
}