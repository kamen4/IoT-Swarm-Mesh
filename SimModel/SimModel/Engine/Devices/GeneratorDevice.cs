using Engine.Core;
using Engine.Packets;
using Engine.Routers;

namespace Engine.Devices;

/// <summary>
/// A device that periodically generates sensor-like data packets and routes
/// them toward the hub. An internal tick counter increments on every engine
/// tick and resets to zero after it reaches <see cref="GenFrequencyTicks"/>;
/// a packet is emitted only at that reset moment, so exactly one packet is
/// sent per <see cref="GenFrequencyTicks"/> ticks.
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
    /// <param name="genFrequency">
    /// The interval in engine ticks at which new packets are generated.
    /// </param>
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
            var packet = new Packet(this, hub, new() { Data = new Random().NextDouble() });
            PacketRouter.Instance.Route(packet, this);
        }
    }

    /// <summary>
    /// Generator devices do not consume incoming packets; this method is a no-op.
    /// </summary>
    /// <param name="packet">The packet addressed to this device (ignored).</param>
    public override void Accept(Packet packet)
    {
    }
}
