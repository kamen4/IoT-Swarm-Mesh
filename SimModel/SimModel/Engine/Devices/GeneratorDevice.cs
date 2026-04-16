using Engine.Core;
using Engine.Packets;

namespace Engine.Devices;

/// <summary>
/// A device that periodically generates sensor-like data packets and routes
/// them toward the hub.
/// <para>
/// Routing is delegated to <see cref="SimulationEngine.RoutePacket"/> so the
/// active <see cref="Engine.Routers.IPacketRouter"/> strategy is always used.
/// </para>
/// </summary>
public class GeneratorDevice : Device
{
    /// <summary>
    /// Gets or sets the number of engine ticks between consecutive packet
    /// generation events.
    /// </summary>
    public long GenFrequencyTicks { get; set; }

    /// <summary>
    /// Gets the number of ticks that have elapsed since the last packet was sent.
    /// Resets to zero after it reaches <see cref="GenFrequencyTicks"/>.
    /// </summary>
    public long CurrentTickCount { get; private set; } = 0;

    /// <summary>
    /// Initialises the generator and subscribes to the engine tick event.
    /// </summary>
    public GeneratorDevice(long genFrequency)
    {
        GenFrequencyTicks = genFrequency;
        SimulationEngine.Instance.TickEvent += Tick;
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (++CurrentTickCount < GenFrequencyTicks)
            return;

        CurrentTickCount = 0;

        var hub = SimulationEngine.Instance.Hub;
        if (hub is not null)
        {
            var packet = new Packet(this, hub, new() { Data = new Random().NextDouble() })
            {
                Direction = PacketDirection.Up,
                MessageType = SwarmMessageType.IO_EVENT,
                DestinationMac = PacketAddress.Clone(hub.MacAddress),
            };

            SimulationEngine.Instance.RoutePacket(packet, this);
        }
    }

    /// <summary>
    /// Generator devices do not consume incoming packets; this method is a no-op.
    /// </summary>
    public override void Accept(Packet packet) { }
}
