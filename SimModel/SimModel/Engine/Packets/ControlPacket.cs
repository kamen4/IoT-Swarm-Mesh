using Engine.Devices;

namespace Engine.Packets;

/// <summary>
/// A command packet sent from the <see cref="HubDevice"/> to a specific
/// <see cref="EmitterDevice"/> to set its on/off state.
/// <para>
/// The hub generates <see cref="ControlPacket"/> instances automatically at
/// the interval configured on each <see cref="EmitterDevice"/> via
/// <see cref="EmitterDevice.ControlFrequencyTicks"/>.
/// When an <see cref="EmitterDevice"/> receives this packet it toggles its
/// <see cref="EmitterDevice.State"/> to <see cref="Command"/>.
/// </para>
/// </summary>
public class ControlPacket : Packet
{
    /// <summary>
    /// Gets the boolean command value  -  <c>true</c> = on, <c>false</c> = off.
    /// </summary>
    public bool Command { get; }

    /// <summary>
    /// Initialises a new <see cref="ControlPacket"/> addressed to
    /// <paramref name="target"/> with the given <paramref name="command"/> value.
    /// </summary>
    /// <param name="hub">The hub device that originates this command.</param>
    /// <param name="target">The emitter device that should receive the command.</param>
    /// <param name="command"><c>true</c> to turn the emitter on; <c>false</c> to turn it off.</param>
    public ControlPacket(Device hub, Device target, bool command)
        : base(hub, target, new PacketData { Data = command })
    {
        Command = command;
    }
}
