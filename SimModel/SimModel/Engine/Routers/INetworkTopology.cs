using Engine.Devices;

namespace Engine.Routers;

/// <summary>
/// Represents the logical topology of the network at a given point in time.
/// <para>
/// The topology separates two concepts that are intentionally kept distinct:
/// <list type="bullet">
///   <item>
///     <term>Visibility</term>
///     <description>
///       Whether two devices are within radio range of each other.
///       Used for broadcast/discovery and as a precondition for establishing a
///       connection. Determined purely by Euclidean distance and
///       <see cref="Engine.Core.SimulationEngine.VisibilityDistance"/>.
///     </description>
///   </item>
///   <item>
///     <term>Connection</term>
///     <description>
///       An explicit, mutually agreed logical link between two devices.
///       A connection can only exist between mutually visible devices, but not
///       every visible device is necessarily connected (subject to per-device
///       connection limits, protocol decisions, etc.).
///       Unicast routing and network-topology algorithms operate on connections,
///       not on raw visibility.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Implementations are owned and updated by the active
/// <see cref="INetworkBuilder"/> and are passed to <see cref="IPacketRouter"/>
/// on every routing call so that routers never need to touch
/// <see cref="Engine.Core.SimulationEngine"/> directly.
/// </para>
/// </summary>
public interface INetworkTopology
{
    /// <summary>
    /// Returns all devices that are within radio range of <paramref name="device"/>,
    /// excluding <paramref name="device"/> itself.
    /// Visibility is a symmetric, distance-based relation and does not imply an
    /// established connection.
    /// </summary>
    /// <param name="device">The device whose visible neighbours are requested.</param>
    /// <returns>Devices within visibility range of <paramref name="device"/>.</returns>
    IEnumerable<Device> GetVisibleDevices(Device device);

    /// <summary>
    /// Returns the devices that are logically connected to <paramref name="device"/>.
    /// <para>
    /// A connection is a persistent, established link managed by the active
    /// <see cref="INetworkBuilder"/>. Only connected neighbours are eligible for
    /// unicast forwarding; broadcast may use visible neighbours instead.
    /// </para>
    /// </summary>
    /// <param name="device">The device whose connected neighbours are requested.</param>
    /// <returns>Devices that have an established connection to <paramref name="device"/>.</returns>
    IEnumerable<Device> GetConnectedDevices(Device device);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="a"/> and <paramref name="b"/> are
    /// within radio range of each other.
    /// </summary>
    bool AreVisible(Device a, Device b);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="a"/> and <paramref name="b"/> have
    /// an established logical connection.
    /// </summary>
    bool AreConnected(Device a, Device b);
}
