using System.Numerics;
using System.Text.Json.Serialization;

namespace Engine.Benchmark;

// ---------------------------------------------------------------------------
// Serialisable device description used inside BenchmarkConfig so the full
// device list can be stored without referencing live Engine.Devices objects.
// ---------------------------------------------------------------------------

/// <summary>Discriminator for the device kind stored in a benchmark configuration.</summary>
public enum BenchmarkDeviceKind { Hub, Generator, Emitter }

/// <summary>
/// Serialisable snapshot of a single device's configuration used inside
/// <see cref="BenchmarkConfig"/>.
/// Converted to a live <see cref="Engine.Devices.Device"/> by
/// <see cref="BenchmarkRunner"/> when it builds the engine state.
/// Declared as a <c>record</c> so Blazor event handlers can use <c>with</c>
/// expressions to create modified copies without mutating shared state.
/// </summary>
public sealed record DeviceBenchmarkDto
{
    /// <summary>Human-readable device name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Device kind (Hub / Generator / Emitter).</summary>
    public BenchmarkDeviceKind Kind { get; set; } = BenchmarkDeviceKind.Generator;

    /// <summary>2D position on the simulation canvas.</summary>
    public float X { get; set; }
    /// <summary>2D position on the simulation canvas.</summary>
    public float Y { get; set; }

    /// <summary>
    /// Packet-generation interval in ticks.
    /// Only used when <see cref="Kind"/> is <see cref="BenchmarkDeviceKind.Generator"/>.
    /// </summary>
    public long GenFrequencyTicks { get; set; } = 40;

    /// <summary>
    /// Automatic hub-control interval in ticks (0 = disabled).
    /// Only used when <see cref="Kind"/> is <see cref="BenchmarkDeviceKind.Emitter"/>.
    /// </summary>
    public long ControlFrequencyTicks { get; set; } = 0;
}
