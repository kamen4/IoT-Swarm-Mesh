using Engine.Devices;
using Engine.Packets;
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
    public IReadOnlyList<Packet> ActivePackets => _packets.UnorderedItems.Select(x => x.Element).ToList();

    /// <summary>
    /// Raised once per call to <see cref="Tick"/>, after the tick counter has
    /// been incremented but before in-flight packets are dispatched.
    /// Devices such as <see cref="GeneratorDevice"/> subscribe to this event
    /// to perform periodic actions.
    /// </summary>
    public event EventHandler? TickEvent;

    // ── Typed simulation events ──────────────────────────────────────────────

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

    /// <summary>
    /// Advances the simulation by one tick.
    /// <list type="number">
    ///   <item>Increments <see cref="TickCount"/>.</item>
    ///   <item>Measures wall-clock delta time since the last tick.</item>
    ///   <item>Fires <see cref="TickEvent"/> so that subscribed devices can act.</item>
    ///   <item>Dispatches all packets that are due to arrive this tick.</item>
    /// </list>
    /// </summary>
    /// <returns>
    /// A tuple of the current <see cref="TickCount"/> and the wall-clock
    /// milliseconds elapsed since the previous tick.
    /// </returns>
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
        while (_packets.Count > 0 && _packets.Peek().ArrivalTick <= TickCount)
        {
            var p = _packets.Dequeue();
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

    /// <summary>
    /// Enqueues a packet into the in-flight priority queue.
    /// The absolute arrival tick is computed as
    /// <c>TickCount + packet.TicksToTravel</c> and stored on the packet.
    /// The queue is a min-heap keyed by arrival tick, so the earliest-arriving
    /// packet is always at the front.
    /// </summary>
    /// <param name="packet">The packet to schedule for delivery.</param>
    public void RegisterPacket(Packet packet)
    {
        var arivalTick = TickCount + packet.TicksToTravel;
        packet.ArrivalTick = arivalTick;
        _packets.Enqueue(packet, arivalTick);
        PacketRegistered?.Invoke(this, new PacketRegisteredEventArgs(packet));
    }

    /// <summary>
    /// Adds a device to the simulation. If the device is a <see cref="HubDevice"/>
    /// it is also stored in <see cref="Hub"/>.
    /// </summary>
    /// <param name="device">The device to register.</param>
    public void RegisterDevice(Device device)
    {
        if (device is HubDevice)
            Hub = device;

        _devices.Add(device);
        DeviceRegistered?.Invoke(this, new DeviceRegisteredEventArgs(device));
    }

    /// <summary>
    /// Removes a previously registered device by its <see cref="Device.Id"/>.
    /// If the removed device was the hub, <see cref="Hub"/> is set to <c>null</c>.
    /// </summary>
    /// <param name="id">The unique identifier of the device to remove.</param>
    public void RemoveDevice(Guid id)
    {
        var device = _devices.FirstOrDefault(d => d.Id == id);
        if (device is null) return;
        _devices.Remove(device);
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
        Hub = null;
        TickCount = 0;
    }

    private double UpdateTime()
    {
        var ret = (DateTime.Now - _lastTickTime).TotalMilliseconds;
        _lastTickTime = DateTime.Now;
        return ret;
    }

    /// <summary>
    /// Returns all devices that are within <see cref="VISIBILITY_DISTANCE"/> of
    /// <paramref name="d"/>, excluding <paramref name="d"/> itself.
    /// Used by <see cref="Routers.PacketRouter"/> to determine broadcast targets.
    /// </summary>
    /// <param name="d">The device whose neighbours are requested.</param>
    /// <returns>An enumerable of visible neighbour devices.</returns>
    public IEnumerable<Device> GetVisibleDevicesFor(Device d)
    {
        foreach (var device in _devices)
        {
            if (device.Id == d.Id) continue;
            if ((device.Position - d.Position).Length() <= VisibilityDistance)
                yield return device;
        }
    }
}
