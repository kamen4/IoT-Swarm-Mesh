using Engine.Devices;

namespace Engine.Routers;

/// <summary>
/// A network-formation strategy that connects every pair of mutually visible
/// devices, producing a full-visibility mesh.
/// <para>
/// This mirrors the original implicit behaviour of the engine where visibility
/// and connectivity were equivalent: if two devices can see each other they
/// are considered connected.
/// </para>
/// <para>
/// Note: as the number of devices grows, the number of edges grows as O(n?).
/// For large networks consider a limited-degree or spanning-tree builder instead.
/// </para>
/// </summary>
public sealed class FullMeshNetworkBuilder : INetworkBuilder
{
    /// <inheritdoc/>
    public string Name => "Full Visibility Mesh";

    /// <inheritdoc/>
    /// <remarks>
    /// Clears all existing connections and then connects every pair
    /// <c>(i, j)</c> where <c>i &lt; j</c> and the two devices are mutually
    /// visible, using <see cref="INetworkTopology.AreVisible"/>.
    /// </remarks>
    public void Build(IReadOnlyList<Device> devices, IMutableNetworkTopology topology)
    {
        topology.ClearConnections();

        for (int i = 0; i < devices.Count; i++)
            for (int j = i + 1; j < devices.Count; j++)
                if (topology.AreVisible(devices[i], devices[j]))
                    topology.Connect(devices[i], devices[j]);
    }
}
