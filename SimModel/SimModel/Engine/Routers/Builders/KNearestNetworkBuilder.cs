using Engine.Devices;
using System.Numerics;

namespace Engine.Routers;

/// <summary>
/// Builds a visibility-constrained k-nearest undirected mesh.
/// </summary>
public sealed class KNearestNetworkBuilder : INetworkBuilder
{
    /// <summary>
    /// Maximum degree target per node.
    /// </summary>
    public int MaxDegree { get; }

    /// <summary>
    /// Creates a k-nearest builder.
    /// </summary>
    /// <param name="maxDegree">Maximum degree target per node.</param>
    public KNearestNetworkBuilder(int maxDegree = 3)
    {
        MaxDegree = Math.Clamp(maxDegree, 1, 16);
    }

    /// <inheritdoc/>
    public string Name => $"K-Nearest (k={MaxDegree})";

    /// <inheritdoc/>
    public void Build(IReadOnlyList<Device> devices, IMutableNetworkTopology topology)
    {
        topology.ClearConnections();
        if (devices.Count <= 1)
            return;

        var degree = devices.ToDictionary(d => d.Id, _ => 0);

        foreach (var device in devices)
        {
            if (degree[device.Id] >= MaxDegree)
                continue;

            var candidates = devices
                .Where(other => other.Id != device.Id)
                .Where(other => topology.AreVisible(device, other))
                .OrderBy(other => Vector2.Distance(device.Position, other.Position))
                .ToArray();

            foreach (var other in candidates)
            {
                if (degree[device.Id] >= MaxDegree)
                    break;

                if (degree[other.Id] >= MaxDegree)
                    continue;

                if (topology.AreConnected(device, other))
                    continue;

                topology.Connect(device, other);
                degree[device.Id]++;
                degree[other.Id]++;
            }
        }
    }
}
