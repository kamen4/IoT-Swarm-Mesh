using Engine.Core;
using Engine.Packets;

namespace Engine.Devices;

/// <summary>
/// A device that receives command packets from the hub and applies a boolean
/// state to itself (e.g. turning an actuator on or off).
/// <para>
/// When <see cref="ControlFrequencyTicks"/> is greater than zero the hub
/// automatically sends a <see cref="ControlPacket"/> to this device every
/// <see cref="ControlFrequencyTicks"/> ticks, toggling its state from on to
/// off and back. Set <see cref="ControlFrequencyTicks"/> to <c>0</c> to
/// disable automatic control signals.
/// </para>
/// </summary>
public class EmitterDevice : Device
{
    /// <summary>Gets the current on/off state of this emitter device.</summary>
    public bool State { get; protected set; }

    /// <summary>
    /// Gets or sets the interval in engine ticks at which the hub sends a
    /// <see cref="ControlPacket"/> that toggles this device's state.
    /// Set to <c>0</c> to disable automatic control signals.
    /// </summary>
    public long ControlFrequencyTicks { get; set; }

    /// <summary>
    /// Gets the number of ticks that have elapsed since the last control
    /// packet was sent. Resets to zero after it reaches
    /// <see cref="ControlFrequencyTicks"/>.
    /// </summary>
    public long CurrentControlTickCount { get; private set; }

    /// <summary>
    /// Initialises the emitter and subscribes to the engine tick event so that
    /// automatic hub-to-device control signals can be sent.
    /// </summary>
    /// <param name="controlFrequencyTicks">
    /// Interval in ticks between automatic control packets from the hub.
    /// Pass <c>0</c> to start with automatic control disabled.
    /// </param>
    public EmitterDevice(long controlFrequencyTicks = 0)
    {
        ControlFrequencyTicks = controlFrequencyTicks;
        SimulationEngine.Instance.TickEvent += OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (ControlFrequencyTicks <= 0)
        {
            CurrentControlTickCount = 0;
            return;
        }

        if (++CurrentControlTickCount < ControlFrequencyTicks)
            return;

        CurrentControlTickCount = 0;

        var hub = SimulationEngine.Instance.Hub;
        if (hub is null) return;

        // Toggle: send the opposite of the current state.
        var packet = new ControlPacket(hub, this, !State);
        SimulationEngine.Instance.RoutePacket(packet, hub);
    }

    /// <summary>
    /// Accepts a packet addressed to this device.
    /// <list type="bullet">
    ///   <item>
    ///     <see cref="ControlPacket"/> — applies <see cref="ControlPacket.Command"/>
    ///     directly to <see cref="State"/>.
    ///   </item>
    ///   <item>
    ///     Any packet whose <see cref="Engine.Packets.PacketData.Data"/> is a
    ///     <c>bool</c> — updates <see cref="State"/> to that value (legacy path).
    ///   </item>
    /// </list>
    /// </summary>
    /// <param name="packet">The packet delivered to this device.</param>
    public override void Accept(Packet packet)
    {
        if (packet is ControlPacket cp)
        {
            State = cp.Command;
            return;
        }

        if (packet.Payload.Data is bool state)
        {
            State = state;
        }
    }
}
