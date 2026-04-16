using Engine.Devices;
using System.Numerics;

namespace Engine.Routers;

/// <summary>
/// Builds a visibility-constrained minimum spanning forest.
/// <para>
/// For each connected visibility component this builder creates exactly
/// <c>n-1</c> links (tree), reducing redundant edges while preserving reachability
/// inside the component.
/// </para>
/// </summary>
public sealed class MinimumSpanningTreeNetworkBuilder : INetworkBuilder
{
    /// <inheritdoc/>
    public string Name => "Visibility MST";

    /// <inheritdoc/>
    public void Build(IReadOnlyList<Device> devices, IMutableNetworkTopology topology)
    {
        topology.ClearConnections();
        if (devices.Count <= 1)
            return;

        var visited = new HashSet<Guid>();

        while (visited.Count < devices.Count)
        {
            var seed = devices.First(d => !visited.Contains(d.Id));
            visited.Add(seed.Id);

            while (true)
            {
                Device? bestFrom = null;
                Device? bestTo = null;
                var bestDistance = float.MaxValue;

                foreach (var from in devices.Where(d => visited.Contains(d.Id)))
                {
                    foreach (var to in devices)
                    {
                        if (visited.Contains(to.Id) || from.Id == to.Id)
                            continue;

                        if (!topology.AreVisible(from, to))
                            continue;

                        var distance = Vector2.Distance(from.Position, to.Position);
                        if (distance >= bestDistance)
                            continue;

                        bestDistance = distance;
                        bestFrom = from;
                        bestTo = to;
                    }
                }

                if (bestFrom is null || bestTo is null)
                    break;

                topology.Connect(bestFrom, bestTo);
                visited.Add(bestTo.Id);
            }
        }
    }
}
