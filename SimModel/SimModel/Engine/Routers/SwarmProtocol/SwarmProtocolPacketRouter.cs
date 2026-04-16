using Engine.Core;
using Engine.Devices;
using Engine.Packets;

namespace Engine.Routers;

/// <summary>
/// Protocol-oriented router that combines charge-based UP forwarding and
/// tree-first DOWN forwarding.
/// <para>
/// Behavior summary:
/// <list type="bullet">
///   <item>Visibility: route decisions are limited to nearest visible top-k neighbors.</item>
///   <item>UP bootstrap: until q_up signal appears, fan-out over the same top-k window as flooding.</item>
///   <item>UP converged: choose one best visible neighbor by observed q_up.</item>
///   <item>DOWN unicast: direct if destination is visible; otherwise forward to eligible children first.</item>
///   <item>DOWN broadcast: forward to eligible children; fallback to one best q_total neighbor.</item>
///   <item>All clones carry previous-hop MAC and charge metadata for neighbor learning.</item>
/// </list>
/// </para>
/// </summary>
public sealed class SwarmProtocolPacketRouter : IPacketRouter
{
    /// <inheritdoc/>
    public string Name => "Swarm Protocol v1.0";

    /// <inheritdoc/>
    public void Route(Packet packet, Device sender, INetworkTopology topology)
    {
        var neighbors = RoutingNeighborPolicy.GetNearestVisibleNeighbors(sender, topology);
        if (neighbors.Count == 0)
            return;

        if (!packet.IsBroadcastDestination &&
            TryRouteDirect(packet, sender, neighbors))
        {
            return;
        }

        if (packet.Direction == PacketDirection.Up)
        {
            RouteUp(packet, sender, neighbors);
            return;
        }

        RouteDown(packet, sender, neighbors);
    }

    private static bool TryRouteDirect(Packet packet, Device sender, IEnumerable<Device> neighbors)
    {
        foreach (var candidate in neighbors)
        {
            if (candidate.Id != packet.To.Id &&
                !PacketAddress.EqualsMac(candidate.MacAddress, packet.DestinationMac))
            {
                continue;
            }

            EnqueueClone(packet, sender, candidate);
            return true;
        }

        return false;
    }

    private static void RouteUp(
        Packet packet,
        Device sender,
        IEnumerable<Device> neighbors)
    {
        var candidates = neighbors
            .Where(c => packet.PreviousHop is null || c.Id != packet.PreviousHop.Id)
            .ToArray();

        if (candidates.Length == 0)
            return;

        var hasChargeSignal = false;
        foreach (var candidate in candidates)
        {
            if (sender.GetNeighborCharge(candidate.MacAddress, PacketDirection.Up) <= 0)
                continue;

            hasChargeSignal = true;
            break;
        }

        // Bootstrap phase: before any charge signal is learned, use the same
        // fan-out neighborhood as flooding routers.
        if (!hasChargeSignal)
        {
            foreach (var candidate in candidates)
                EnqueueClone(packet, sender, candidate);

            return;
        }

        Device? best = null;
        ushort bestCharge = 0;

        foreach (var candidate in candidates)
        {
            var charge = sender.GetNeighborCharge(candidate.MacAddress, PacketDirection.Up);

            if (best is null || charge > bestCharge)
            {
                best = candidate;
                bestCharge = charge;
                continue;
            }

            if (charge == bestCharge &&
                PacketAddress.Compare(candidate.MacAddress, best.MacAddress) < 0)
            {
                best = candidate;
            }
        }

        if (best is null)
            return;

        EnqueueClone(packet, sender, best);
    }

    private static void RouteDown(
        Packet packet,
        Device sender,
        IEnumerable<Device> neighbors)
    {
        if (packet.IsBroadcastDestination)
        {
            RouteDownBroadcast(packet, sender, neighbors);
            return;
        }

        RouteDownUnicast(packet, sender, neighbors);
    }

    private static void RouteDownUnicast(
        Packet packet,
        Device sender,
        IEnumerable<Device> neighbors)
    {
        var children = neighbors
            .Where(d => d.HasParent(sender) && d.IsForwardEligible)
            .Where(d => packet.PreviousHop is null || d.Id != packet.PreviousHop.Id)
            .ToArray();

        if (children.Length > 0)
        {
            Array.Sort(children, static (a, b) => PacketAddress.Compare(a.MacAddress, b.MacAddress));
            foreach (var child in children)
                EnqueueClone(packet, sender, child);
            return;
        }

        var fallback = SelectBestTotalNeighbor(packet, sender, neighbors);
        if (fallback is null)
            return;

        EnqueueClone(packet, sender, fallback);
    }

    private static void RouteDownBroadcast(
        Packet packet,
        Device sender,
        IEnumerable<Device> neighbors)
    {
        var children = neighbors
            .Where(d => d.HasParent(sender) && d.IsForwardEligible)
            .Where(d => packet.PreviousHop is null || d.Id != packet.PreviousHop.Id)
            .ToArray();

        if (children.Length > 0)
        {
            Array.Sort(children, static (a, b) => PacketAddress.Compare(a.MacAddress, b.MacAddress));
            foreach (var child in children)
                EnqueueClone(packet, sender, child);
            return;
        }

        var fallback = SelectBestTotalNeighbor(packet, sender, neighbors);
        if (fallback is null)
            return;

        EnqueueClone(packet, sender, fallback);
    }

    private static Device? SelectBestTotalNeighbor(
        Packet packet,
        Device sender,
        IEnumerable<Device> neighbors)
    {
        Device? best = null;
        ushort bestCharge = 0;

        foreach (var candidate in neighbors)
        {
            if (packet.PreviousHop is not null && candidate.Id == packet.PreviousHop.Id)
                continue;

            var charge = sender.GetNeighborCharge(candidate.MacAddress, PacketDirection.Down);

            if (best is null || charge > bestCharge)
            {
                best = candidate;
                bestCharge = charge;
                continue;
            }

            if (charge == bestCharge &&
                PacketAddress.Compare(candidate.MacAddress, best.MacAddress) < 0)
            {
                best = candidate;
            }
        }

        return best;
    }

    private static void EnqueueClone(Packet packet, Device sender, Device nextHop)
    {
        var clone = packet.Clone();
        clone.NextHop = nextHop;
        clone.PreviousHop = sender;
        clone.PreviousHopMac = PacketAddress.Clone(sender.MacAddress);
        clone.AdvertisedCharge = packet.Direction == PacketDirection.Up
            ? sender.QUpSelf
            : sender.QTotalSelf;
        clone.DecayEpochHint = sender.LastDecayEpoch;

        SimulationEngine.Instance.RegisterPacket(clone);
    }
}
