using Engine.Devices;
using Engine.Packets;
using System.Numerics;

namespace Engine.Routers;

/// <summary>
/// Shared deterministic neighbor selection policy for all router strategies.
/// </summary>
internal static class RoutingNeighborPolicy
{
    /// <summary>
    /// Maximum count of visible neighbors considered per hop.
    /// </summary>
    public const int DefaultMaxVisibleNeighbors = 10;

    /// <summary>
    /// Returns up to <paramref name="maxVisibleNeighbors"/> nearest visible
    /// neighbors around <paramref name="sender"/>, ordered by squared
    /// distance then MAC key for deterministic ties.
    /// </summary>
    /// <param name="sender">Current forwarding node.</param>
    /// <param name="topology">Network topology view.</param>
    /// <param name="maxVisibleNeighbors">Neighbor cap.</param>
    /// <returns>Deterministically ordered nearest visible neighbors.</returns>
    public static IReadOnlyList<Device> GetNearestVisibleNeighbors(
        Device sender,
        INetworkTopology topology,
        int maxVisibleNeighbors = DefaultMaxVisibleNeighbors)
    {
        var cap = Math.Max(1, maxVisibleNeighbors);

        return topology
            .GetVisibleDevices(sender)
            .OrderBy(d => Vector2.DistanceSquared(sender.Position, d.Position))
            .ThenBy(d => PacketAddress.ToKey(d.MacAddress), StringComparer.Ordinal)
            .Take(cap)
            .ToArray();
    }
}
