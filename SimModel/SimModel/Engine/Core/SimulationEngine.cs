using Engine.Devices;
using Engine.Packets;
using Engine.Routers;
using System;

namespace Engine.Core;

/// <summary>
/// The central singleton that drives the entire simulation.
/// It maintains the device registry, owns the tick-based time model, and
/// manages the priority queue of in-flight packets.
/// <para>
/// Every call to <see cref="Tick"/> advances the simulation by one step:
/// the tick counter is incremented, wall-clock delta time is measured,
/// all subscribers to <see cref="TickEvent"/> are notified, and any packets
/// whose <see cref="Packet.ArrivalTick"/> has been reached are dequeued and
/// dispatched to their <see cref="Packet.NextHop"/> device.
/// </para>
/// <para>
/// Routing strategy and network-formation strategy are fully replaceable at
/// runtime via <see cref="Router"/> and <see cref="NetworkBuilder"/>.
/// The read-only topology view is exposed through <see cref="Topology"/>.
/// </para>
/// </summary>
public class SimulationEngine
{
    /// <summary>
    /// The default visibility distance used when none is configured explicitly.
    /// </summary>
    public const int VISIBILITY_DISTANCE = 200;

    /// <summary>
    /// The effective visibility distance for this simulation instance.
    /// Defaults to <see cref="VISIBILITY_DISTANCE"/>; can be changed at runtime.
    /// Changing this value does <em>not</em> automatically rebuild the topology —
    /// call <see cref="RebuildTopology"/> explicitly if needed.
    /// </summary>
    public int VisibilityDistance { get; set; } = VISIBILITY_DISTANCE;

    /// <summary>Gets the singleton instance of <see cref="SimulationEngine"/>.</summary>
    public static SimulationEngine Instance { get; } = new();

    private DateTime _lastTickTime = DateTime.Now;

    /// <summary>Gets the total number of ticks that have elapsed since the simulation started.</summary>
    public long TickCount { get; private set; } = 0;

    /// <summary>
    /// Gets the hub device registered in this simulation, or <c>null</c> if no
    /// <see cref="HubDevice"/> has been added yet.
    /// </summary>
    public Device? Hub { get; private set; }

    private readonly PriorityQueue<Packet, long> _packets = new();
    private readonly List<Device> _devices = [];

    /// <summary>Gets a read-only snapshot of all devices registered in the simulation.</summary>
    public IReadOnlyList<Device> Devices => _devices;

    /// <summary>Gets a read-only snapshot of all packets currently in-flight.</summary>
    /// <remarks>
    /// This allocates a new <see cref="List{T}"/> on every call.
    /// Use <see cref="ActivePacketsCount"/> when only the count is needed.
    /// </remarks>
    public IReadOnlyList<Packet> ActivePackets => _packets.UnorderedItems.Select(x => x.Element).ToList();

    /// <summary>
    /// Gets the number of packets currently in-flight without allocating a list.
    /// Prefer this over <c>ActivePackets.Count</c> in hot paths such as
    /// statistics collection.
    /// </summary>
    public int ActivePacketsCount => _packets.Count;

    // -------------------------------------------------------------------------
    // Pluggable strategies
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the active packet-routing strategy.
    /// Defaults to <see cref="FloodingPacketRouter"/>.
    /// Can be swapped at runtime (e.g. from the UI) to compare different
    /// routing protocols on the same network without restarting the simulation.
    /// </summary>
    public IPacketRouter Router { get; set; } = new FloodingPacketRouter();

    /// <summary>
    /// Gets or sets the active network-formation strategy.
    /// Defaults to <see cref="FullMeshNetworkBuilder"/>.
    /// Changing this property does <em>not</em> automatically rebuild the
    /// topology; call <see cref="RebuildTopology"/> to apply the new builder
    /// to the current device set.
    /// </summary>
    public INetworkBuilder NetworkBuilder { get; set; } = new FullMeshNetworkBuilder();

    /// <summary>
    /// Gets the read-only view of the current network topology (visibility and
    /// established connections).
    /// Always up-to-date: visibility is computed on-the-fly, connections are
    /// rebuilt by <see cref="NetworkBuilder"/> on every device-registry change.
    /// </summary>
    public INetworkTopology Topology => _topology;

    private readonly NetworkTopology _topology;

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Raised once per call to <see cref="Tick"/>, after the tick counter has
    /// been incremented but before in-flight packets are dispatched.
    /// </summary>
    public event EventHandler? TickEvent;

    /// <summary>Raised every time a packet is enqueued (including flood clones).</summary>
    public event EventHandler<PacketRegisteredEventArgs>? PacketRegistered;

    /// <summary>Raised when a packet's TTL reaches zero and it is silently dropped.</summary>
    public event EventHandler<PacketExpiredEventArgs>? PacketExpired;

    /// <summary>Raised when a packet reaches its intended destination device.</summary>
    public event EventHandler<PacketDeliveredEventArgs>? PacketDelivered;

    /// <summary>Raised after a new device is added to the simulation.</summary>
    public event EventHandler<DeviceRegisteredEventArgs>? DeviceRegistered;

    /// <summary>Raised after a device is removed from the simulation.</summary>
    public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;

    /// <summary>Raised once per <see cref="Tick"/> call after all packets are dispatched.</summary>
    public event EventHandler<TickedEventArgs>? Ticked;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    private SimulationEngine()
    {
        _topology = new NetworkTopology(this);
    }

    // -------------------------------------------------------------------------
    // Tick loop
    // -------------------------------------------------------------------------

    /// <summary>
    /// Advances the simulation by one tick.
    /// <list type="number">
    ///   <item>Increments <see cref="TickCount"/>.</item>
    ///   <item>Measures wall-clock delta time since the last tick.</item>
    ///   <item>Fires <see cref="TickEvent"/> so that subscribed devices can act.</item>
    ///   <item>Dispatches all packets that are due to arrive this tick.</item>
    ///   <item>Raises <see cref="Ticked"/>.</item>
    /// </list>
    /// </summary>
    public (long tick, double dt) Tick()
    {
        ++TickCount;
        var dt = UpdateTime();
        TickEvent?.Invoke(this, EventArgs.Empty);

        TickPackets();

        Ticked?.Invoke(this, new TickedEventArgs(TickCount, dt));
        return (TickCount, dt);
    }

    private void TickPackets()
    {
        // Snapshot the set of packets due this tick before iterating.
        // Recieve() → RoutePacket() → RegisterPacket() adds new packets into
        // _packets while we process; those are scheduled for FUTURE ticks
        // (ArrivalTick = TickCount + TicksToTravel, and TicksToTravel >= 1),
        // so they will never satisfy ArrivalTick <= TickCount for the current
        // tick. However, collecting upfront keeps the loop bounds predictable
        // and prevents any edge-case where a packet with TicksToTravel == 0
        // could be dispatched in the same tick it was enqueued.
        var due = new List<Packet>();
        while (_packets.Count > 0 && _packets.Peek().ArrivalTick <= TickCount)
            due.Add(_packets.Dequeue());

        foreach (var p in due)
        {
            if (--p.TTL > 0)
            {
                if (p.To.Equals(p.NextHop))
                    PacketDelivered?.Invoke(this, new PacketDeliveredEventArgs(p));

                p.NextHop.Recieve(p);
            }
            else
            {
                PacketExpired?.Invoke(this, new PacketExpiredEventArgs(p));
            }
        }
    }

    // -------------------------------------------------------------------------
    // Packet limit
    // -------------------------------------------------------------------------

    /// <summary>
    /// The multiplier used to compute the automatic packet limit when
    /// <see cref="MaxActivePackets"/> is set to <c>0</c>.
    /// Default: <c>2000</c> packets per registered device.
    /// </summary>
    public const int AUTO_PACKET_LIMIT_PER_DEVICE = 2000;

    /// <summary>
    /// Gets or sets the maximum number of packets that may be simultaneously
    /// in-flight in the simulation.
    /// <para>
    /// When set to <c>0</c> (the default) the limit is computed automatically
    /// as <c>Devices.Count × <see cref="AUTO_PACKET_LIMIT_PER_DEVICE"/></c>
    /// each time a packet is enqueued, so it scales with the network size.
    /// </para>
    /// <para>
    /// When the in-flight count reaches or exceeds the effective limit,
    /// <see cref="RegisterPacket"/> throws
    /// <see cref="PacketLimitExceededException"/> and the tick loop must be
    /// stopped by the caller.
    /// Set to a positive value to override the automatic limit.
    /// </para>
    /// </summary>
    public int MaxActivePackets { get; set; } = 0;

    /// <summary>
    /// Returns the effective packet limit that will be enforced on the next
    /// <see cref="RegisterPacket"/> call.
    /// <list type="bullet">
    ///   <item>If <see cref="MaxActivePackets"/> &gt; 0 — returns that value.</item>
    ///   <item>Otherwise — returns <c>Devices.Count × <see cref="AUTO_PACKET_LIMIT_PER_DEVICE"/></c>.</item>
    /// </list>
    /// Returns <see cref="int.MaxValue"/> when there are no devices yet so that
    /// early registration during setup never triggers the guard.
    /// </summary>
    public int EffectivePacketLimit =>
        MaxActivePackets > 0
            ? MaxActivePackets
            : _devices.Count > 0
                ? _devices.Count * AUTO_PACKET_LIMIT_PER_DEVICE
                : int.MaxValue;

    // -------------------------------------------------------------------------
    // Packet scheduling
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enqueues a packet into the in-flight priority queue.
    /// The absolute arrival tick is computed as
    /// <c>TickCount + packet.TicksToTravel</c> and stored on the packet.
    /// </summary>
    /// <exception cref="PacketLimitExceededException">
    /// Thrown when the number of in-flight packets is already at or above
    /// <see cref="EffectivePacketLimit"/> before the new packet is added.
    /// The caller (typically the simulation tick loop) must stop the simulation
    /// and surface this error to the user.
    /// </exception>
    public void RegisterPacket(Packet packet)
    {
        // Use ActivePacketsCount (O(1)) instead of ActivePackets.Count (O(N) alloc).
        var currentCount = ActivePacketsCount;
        var limit        = EffectivePacketLimit;

        if (currentCount >= limit)
            throw new PacketLimitExceededException(limit, currentCount, TickCount);

        var arivalTick = TickCount + packet.TicksToTravel;
        packet.ArrivalTick = arivalTick;
        _packets.Enqueue(packet, arivalTick);
        PacketRegistered?.Invoke(this, new PacketRegisteredEventArgs(packet));
    }

    // -------------------------------------------------------------------------
    // Device registry
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a device to the simulation. If the device is a <see cref="HubDevice"/>
    /// it is also stored in <see cref="Hub"/>.
    /// The topology is rebuilt via <see cref="RebuildTopology"/> after the device
    /// is added.
    /// </summary>
    public void RegisterDevice(Device device)
    {
        if (device is HubDevice)
            Hub = device;

        _devices.Add(device);
        RebuildTopology();
        DeviceRegistered?.Invoke(this, new DeviceRegisteredEventArgs(device));
    }

    /// <summary>
    /// Removes a previously registered device by its <see cref="Device.Id"/>.
    /// The topology is updated (device connections removed) and then rebuilt.
    /// </summary>
    public void RemoveDevice(Guid id)
    {
        var device = _devices.FirstOrDefault(d => d.Id == id);
        if (device is null) return;
        _devices.Remove(device);
        _topology.RemoveDevice(device);
        RebuildTopology();
        if (Hub?.Id == id) Hub = null;
        DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(id));
    }

    /// <summary>
    /// Removes all registered devices and discards all in-flight packets,
    /// resetting the engine to its initial empty state.
    /// </summary>
    public void Reset()
    {
        _devices.Clear();
        _packets.Clear();
        _topology.ClearConnections();
        Hub = null;
        TickCount = 0;
        // Re-stamp so the next Tick() doesn't inherit stale wall-clock time.
        _lastTickTime = DateTime.Now;
    }

    // -------------------------------------------------------------------------
    // Topology management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Triggers the active <see cref="NetworkBuilder"/> to recompute the logical
    /// connection graph for the current device set.
    /// <para>
    /// Called automatically on every device-registry change
    /// (<see cref="RegisterDevice"/>, <see cref="RemoveDevice"/>).
    /// Can also be called manually — for example after moving a device or after
    /// replacing <see cref="NetworkBuilder"/> — to immediately reflect the new
    /// topology without waiting for the next device change.
    /// </para>
    /// </summary>
    public void RebuildTopology()
    {
        NetworkBuilder.Build(_devices, _topology);
    }

    // -------------------------------------------------------------------------
    // Routing entry-point (called by Device.Recieve)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Forwards <paramref name="packet"/> from <paramref name="sender"/> using
    /// the currently active <see cref="Router"/> and the current
    /// <see cref="Topology"/>.
    /// </summary>
    internal void RoutePacket(Packet packet, Device sender)
    {
        Router.Route(packet, sender, _topology);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private double UpdateTime()
    {
        var ret = (DateTime.Now - _lastTickTime).TotalMilliseconds;
        _lastTickTime = DateTime.Now;
        return ret;
    }
}
