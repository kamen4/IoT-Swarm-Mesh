using Engine.Devices;
using Engine.Packets;

namespace Engine.Routers;

/// <summary>
/// Defines the contract for a packet routing strategy.
/// <para>
/// A router receives a packet from a forwarding device and is responsible for
/// deciding which device(s) the packet should be sent to next.
/// Implementations may use different strategies: flooding broadcast,
/// shortest-path unicast, gradient routing, etc.
/// </para>
/// <para>
/// The active router is set on <see cref="Engine.Core.SimulationEngine"/> and is
/// consulted by every device whenever it needs to forward a packet.
/// Swapping the router at runtime (e.g. from UI) immediately changes routing
/// behaviour for all subsequent packets, which enables side-by-side protocol
/// comparison on the same network.
/// </para>
/// </summary>
public interface IPacketRouter
{
    /// <summary>
    /// Gets the human-readable name of this routing strategy.
    /// Shown in the UI when the user selects a routing protocol.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Routes <paramref name="packet"/> from <paramref name="sender"/>.
    /// The implementation decides which device(s) receive clones of the packet
    /// on the next hop.
    /// </summary>
    /// <param name="packet">The packet to route.</param>
    /// <param name="sender">The device currently holding the packet.</param>
    /// <param name="topology">
    /// The current network topology. Used to discover neighbours or established
    /// connections reachable from <paramref name="sender"/>.
    /// </param>
    void Route(Packet packet, Device sender, INetworkTopology topology);
}
