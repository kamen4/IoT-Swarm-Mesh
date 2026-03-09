using System.Text.Json.Serialization;

namespace Engine.Benchmark;

// ---------------------------------------------------------------------------
// Discriminated-union for scheduled simulation events.
// Each variant is a separate sealed record so JSON serialization with
// $type discrimination works cleanly, and callers can pattern-match.
// ---------------------------------------------------------------------------

/// <summary>
/// Base type for all events that can be scheduled in a
/// <see cref="BenchmarkConfig"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ToggleBenchmarkEvent),     "toggle")]
[JsonDerivedType(typeof(RemoveDeviceBenchmarkEvent),"removeDevice")]
[JsonDerivedType(typeof(AddDeviceBenchmarkEvent),   "addDevice")]
public abstract record BenchmarkEvent;

/// <summary>
/// Hub sends a toggle (on/off) <see cref="Engine.Packets.ControlPacket"/> to the
/// named emitter device at the scheduled tick.
/// </summary>
/// <param name="DeviceName">
/// The <see cref="Engine.Devices.Device.Name"/> of the target emitter.
/// If no such device exists in the engine at the scheduled tick the event is
/// silently skipped.
/// </param>
public sealed record ToggleBenchmarkEvent(string DeviceName) : BenchmarkEvent;

/// <summary>
/// Removes the named device from the engine at the scheduled tick, simulating
/// a node failure or graceful leave.
/// </summary>
/// <param name="DeviceName">Name of the device to remove.</param>
public sealed record RemoveDeviceBenchmarkEvent(string DeviceName) : BenchmarkEvent;

/// <summary>
/// Adds a new device to the engine at the scheduled tick, simulating a node
/// joining the network mid-simulation.
/// </summary>
/// <param name="Device">
/// A <see cref="DeviceBenchmarkDto"/> describing the device to add.
/// </param>
public sealed record AddDeviceBenchmarkEvent(DeviceBenchmarkDto Device) : BenchmarkEvent;

// ---------------------------------------------------------------------------
// Wraps a single scheduled event together with its trigger tick.
// ---------------------------------------------------------------------------

/// <summary>
/// Associates a <see cref="BenchmarkEvent"/> with the engine tick at which it
/// should be fired during a benchmark run.
/// </summary>
/// <param name="AtTick">
/// Engine tick (1-based, matching <see cref="Engine.Core.SimulationEngine.TickCount"/>)
/// at which <see cref="Event"/> is applied.
/// </param>
/// <param name="Event">The event to apply.</param>
public sealed record BenchmarkEventEntry(long AtTick, BenchmarkEvent Event);
