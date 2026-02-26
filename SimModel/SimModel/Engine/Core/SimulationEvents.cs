using Engine.Devices;
using Engine.Packets;

namespace Engine.Core;

/// <summary>Event args for <see cref="SimulationEngine.PacketRegistered"/>.</summary>
public sealed class PacketRegisteredEventArgs(Packet packet) : EventArgs
{
    /// <summary>Gets the packet that was enqueued.</summary>
    public Packet Packet { get; } = packet;
}

/// <summary>Event args for <see cref="SimulationEngine.PacketExpired"/> (TTL reached zero).</summary>
public sealed class PacketExpiredEventArgs(Packet packet) : EventArgs
{
    /// <summary>Gets the packet that was silently dropped.</summary>
    public Packet Packet { get; } = packet;
}

/// <summary>Event args for <see cref="SimulationEngine.PacketDelivered"/> (reached its destination).</summary>
public sealed class PacketDeliveredEventArgs(Packet packet) : EventArgs
{
    /// <summary>Gets the packet that was successfully delivered to its destination device.</summary>
    public Packet Packet { get; } = packet;
}

/// <summary>Event args for <see cref="SimulationEngine.DeviceRegistered"/>.</summary>
public sealed class DeviceRegisteredEventArgs(Device device) : EventArgs
{
    /// <summary>Gets the device that was added to the simulation.</summary>
    public Device Device { get; } = device;
}

/// <summary>Event args for <see cref="SimulationEngine.DeviceRemoved"/>.</summary>
public sealed class DeviceRemovedEventArgs(Guid deviceId) : EventArgs
{
    /// <summary>Gets the <see cref="Guid"/> of the device that was removed.</summary>
    public Guid DeviceId { get; } = deviceId;
}

/// <summary>Event args for <see cref="SimulationEngine.Ticked"/>.</summary>
public sealed class TickedEventArgs(long tickCount, double dtMs) : EventArgs
{
    /// <summary>Gets the engine tick counter value at the moment the tick completed.</summary>
    public long TickCount { get; } = tickCount;

    /// <summary>Gets the wall-clock milliseconds elapsed between the previous tick and this one.</summary>
    public double DtMs { get; } = dtMs;
}
