using Engine.Core;
using Engine.Devices;
using Engine.Packets;

namespace Engine.Routers;

/// <summary>
/// An improved flooding router that reduces redundant packet copies by applying
/// three optimisations over the naive <see cref="FloodingPacketRouter"/>:
/// <list type="number">
///   <item>
///     <term>Direct delivery</term>
///     <description>
///       If the intended destination (<see cref="Packet.To"/>) is directly
///       visible from <paramref name="sender"/>, the packet is sent
///       <em>only</em> to that device  -  no unnecessary flood copies are made.
///     </description>
///   </item>
///   <item>
///     <term>No reverse path</term>
///     <description>
///       The packet is never forwarded back to <see cref="Packet.PreviousHop"/>
///       (the device that just sent it here). This eliminates the most common
///       source of duplicate copies in bidirectional flooding.
///     </description>
///   </item>
///   <item>
///     <term>No source echo</term>
///     <description>
///       The packet is never forwarded back to <see cref="Packet.From"/>
///       (the original source device), preventing useless round-trips.
///     </description>
///   </item>
/// </list>
/// <para>
/// Together these rules drastically shrink the clone fan-out compared to the
/// naive flood, especially for unicast packets such as
/// <see cref="Engine.Packets.ControlPacket"/>, while still guaranteeing delivery
/// in any connected topology with sufficient TTL.
/// </para>
/// </summary>
public class SmartFloodingPacketRouter : IPacketRouter
{
    /// <inheritdoc/>
    public string Name => "Smart Flooding";

    /// <inheritdoc/>
    public void Route(Packet packet, Device sender, INetworkTopology topology)
    {
        var visibleDevices = topology.GetVisibleDevices(sender);

        // Rule 1  -  direct delivery: if the destination is a direct neighbour,
        // send only to it and skip the full flood entirely.
        foreach (var d in visibleDevices)
        {
            if (d.Id == packet.To.Id)
            {
                var direct = packet.Clone();
                direct.NextHop = d;
                SimulationEngine.Instance.RegisterPacket(direct);
                return;
            }
        }

        // Rules 2 & 3  -  filtered flood: broadcast to all visible neighbours
        // except the device the packet just came from (PreviousHop) and the
        // original source (From). Both exclusions prevent the most common
        // duplicate paths without requiring any routing table.
        foreach (var d in visibleDevices)
        {
            // Skip the node that forwarded the packet here (reverse path).
            if (packet.PreviousHop is not null && d.Id == packet.PreviousHop.Id)
                continue;

            // Skip the original source of the packet.
            if (d.Id == packet.From.Id)
                continue;

            var p = packet.Clone();
            p.NextHop = d;
            SimulationEngine.Instance.RegisterPacket(p);
        }
    }
}
