using Engine.Devices;

namespace Engine.Routers;

/// <summary>
/// Defines the contract for a network-formation (mesh-building) strategy.
/// <para>
/// A network builder is responsible for deciding which devices establish
/// explicit connections with each other, forming the logical mesh topology.
/// It operates on top of raw visibility data and produces an
/// <see cref="INetworkTopology"/> that the active <see cref="IPacketRouter"/>
/// then uses for forwarding decisions.
/// </para>
/// <para>
/// Examples of algorithms that implement this interface:
/// <list type="bullet">
///   <item>Full-mesh — every visible pair is connected (current implicit behaviour).</item>
///   <item>Limited-degree mesh — each device connects to at most <c>K</c> neighbours.</item>
///   <item>Minimum spanning tree — minimises total edge weight.</item>
///   <item>Cluster-head / LEACH-style hierarchy.</item>
/// </list>
/// </para>
/// <para>
/// Like <see cref="IPacketRouter"/>, the active builder is stored on
/// <see cref="Engine.Core.SimulationEngine"/> and can be swapped at runtime from the
/// UI to compare how different formation strategies affect routing statistics.
/// </para>
/// </summary>
public interface INetworkBuilder
{
    /// <summary>
    /// Gets the human-readable name of this network-formation strategy.
    /// Shown in the UI when the user selects a mesh protocol.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Rebuilds the logical topology for the given set of devices.
    /// Called by <see cref="Engine.Core.SimulationEngine"/> whenever the device
    /// registry changes (device added / removed) or when an explicit rebuild is
    /// requested (e.g. on topology-change events in future dynamic-mesh
    /// protocols).
    /// </summary>
    /// <param name="devices">
    /// The current full device list from <see cref="Engine.Core.SimulationEngine.Devices"/>.
    /// The builder must treat this as a read-only snapshot.
    /// </param>
    /// <param name="topology">
    /// The topology object to update. The builder calls its mutating methods to
    /// add or remove connections; it must not replace the instance itself because
    /// routers and statistics may hold a reference to it.
    /// </param>
    void Build(IReadOnlyList<Device> devices, IMutableNetworkTopology topology);
}
