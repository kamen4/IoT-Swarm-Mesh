using Engine.Devices;

namespace Engine.Packets;

/// <summary>
/// Carries the application-level payload of a <see cref="Packet"/>.
/// The <see cref="Data"/> property is intentionally untyped so that any
/// value (sensor readings, command flags, raw bytes, etc.) can be transported
/// without the engine imposing a schema.
/// </summary>
public class PacketData
{
    /// <summary>
    /// Gets or sets the payload value. The consuming device is responsible
    /// for casting this to the expected type.
    /// </summary>
    public object? Data { get; set; }
}