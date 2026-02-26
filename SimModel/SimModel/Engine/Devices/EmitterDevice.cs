using Engine.Packets;

namespace Engine.Devices;

/// <summary>
/// A device that receives command packets from the hub and applies a boolean
/// state to itself (e.g. turning an actuator on or off).
/// When an accepted packet carries a <c>bool</c> payload the device updates
/// its <see cref="State"/> accordingly.
/// </summary>
public class EmitterDevice : Device
{
    /// <summary>Gets the current on/off state of this emitter device.</summary>
    public bool State { get; protected set; }

    /// <summary>
    /// Accepts a packet and, if the payload is a <c>bool</c> value, updates
    /// <see cref="State"/> to that value.
    /// </summary>
    /// <param name="packet">The packet delivered to this device.</param>
    public override void Accept(Packet packet)
    {
        if (packet.Payload.Data is bool state)
        {
            State = state;
        }
    }
}
