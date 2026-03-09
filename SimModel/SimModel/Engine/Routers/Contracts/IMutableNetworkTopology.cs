using Engine.Devices;

namespace Engine.Routers;

/// <summary>
/// Extends <see cref="INetworkTopology"/> with mutation methods so that an
/// <see cref="INetworkBuilder"/> can update the connection graph without
/// exposing mutation to routers or other consumers.
/// <para>
/// Only <see cref="INetworkBuilder"/> implementations and
/// <see cref="Engine.Core.SimulationEngine"/> should hold a reference to
/// <see cref="IMutableNetworkTopology"/>. All other code (routers, statistics,
/// UI) works through the read-only <see cref="INetworkTopology"/> projection.
/// </para>
/// </summary>
public interface IMutableNetworkTopology : INetworkTopology
{
    /// <summary>
    /// Establishes a bidirectional logical connection between <paramref name="a"/>
    /// and <paramref name="b"/>.
    /// Has no effect if the connection already exists.
    /// </summary>
    void Connect(Device a, Device b);

    /// <summary>
    /// Removes the logical connection between <paramref name="a"/> and
    /// <paramref name="b"/>.
    /// Has no effect if no connection exists.
    /// </summary>
    void Disconnect(Device a, Device b);

    /// <summary>
    /// Removes all connections involving <paramref name="device"/>.
    /// Called when a device is removed from the simulation.
    /// </summary>
    void RemoveDevice(Device device);

    /// <summary>
    /// Removes all established connections, leaving the connection graph empty.
    /// Visibility relationships are recomputed from device positions and are
    /// not affected.
    /// </summary>
    void ClearConnections();
}
