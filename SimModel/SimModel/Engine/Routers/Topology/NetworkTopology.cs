using Engine.Core;
using Engine.Devices;
using System.Numerics;

namespace Engine.Routers;

/// <summary>
/// Default in-memory implementation of <see cref="IMutableNetworkTopology"/>.
/// <para>
/// Visibility is computed on-the-fly from device positions and
/// <see cref="SimulationEngine.VisibilityDistance"/>; it is never cached
/// so it always reflects the current layout.
/// </para>
/// <para>
/// Connections are stored as an adjacency set (both directions) and are
/// managed exclusively by the active <see cref="INetworkBuilder"/>.
/// </para>
/// </summary>
public sealed class NetworkTopology : IMutableNetworkTopology
{
    private readonly SimulationEngine _engine;

    /// <summary>
    /// Adjacency set: key device ? set of devices it is connected to.
    /// Stored bidirectionally so both directions can be queried in O(1).
    /// </summary>
    private readonly Dictionary<Guid, HashSet<Guid>> _connections = new();

    /// <summary>
    /// Initialises the topology bound to the given <paramref name="engine"/>
    /// for visibility distance lookups.
    /// </summary>
    public NetworkTopology(SimulationEngine engine)
    {
        _engine = engine;
    }

    // -------------------------------------------------------------------------
    // INetworkTopology  -  read-only projection
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public IEnumerable<Device> GetVisibleDevices(Device device)
    {
        foreach (var d in _engine.Devices)
        {
            if (d.Id == device.Id) continue;
            if (IsWithinRange(d.Position, device.Position))
                yield return d;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<Device> GetConnectedDevices(Device device)
    {
        if (!_connections.TryGetValue(device.Id, out var neighbours))
            yield break;

        foreach (var id in neighbours)
        {
            var d = _engine.Devices.FirstOrDefault(x => x.Id == id);
            if (d is not null)
                yield return d;
        }
    }

    /// <inheritdoc/>
    public bool AreVisible(Device a, Device b)
        => IsWithinRange(a.Position, b.Position);

    /// <inheritdoc/>
    public bool AreConnected(Device a, Device b)
        => _connections.TryGetValue(a.Id, out var set) && set.Contains(b.Id);

    // -------------------------------------------------------------------------
    // IMutableNetworkTopology  -  builder-only mutations
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public void Connect(Device a, Device b)
    {
        GetOrCreate(a.Id).Add(b.Id);
        GetOrCreate(b.Id).Add(a.Id);
    }

    /// <inheritdoc/>
    public void Disconnect(Device a, Device b)
    {
        if (_connections.TryGetValue(a.Id, out var sa)) sa.Remove(b.Id);
        if (_connections.TryGetValue(b.Id, out var sb)) sb.Remove(a.Id);
    }

    /// <inheritdoc/>
    public void RemoveDevice(Device device)
    {
        if (!_connections.TryGetValue(device.Id, out var neighbours))
            return;

        foreach (var neighbourId in neighbours)
            if (_connections.TryGetValue(neighbourId, out var ns))
                ns.Remove(device.Id);

        _connections.Remove(device.Id);
    }

    /// <inheritdoc/>
    public void ClearConnections() => _connections.Clear();

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private bool IsWithinRange(Vector2 a, Vector2 b)
        => (a - b).Length() <= _engine.VisibilityDistance;

    private HashSet<Guid> GetOrCreate(Guid id)
    {
        if (!_connections.TryGetValue(id, out var set))
            _connections[id] = set = new HashSet<Guid>();
        return set;
    }
}
