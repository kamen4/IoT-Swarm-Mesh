using Engine.Core;
using Engine.Packets;
using Engine.Routers;

namespace Engine.Devices;

public class GeneratorDevice : Device
{
    public long GenFrequencyTicks { get; set; }

    ///
    public GeneratorDevice(long genFrequency)
    {
        GenFrequencyTicks = genFrequency;
        SimulationEngine.Instance.TickEvent += Tick;
    }

    private void Tick(object? sender, EventArgs e)
    {
        var hub = SimulationEngine.Instance.Hub;
        if (hub is not null)
        {
            var packet = new Packet(this, hub, new() { Data = new Random().NextDouble() });
            PacketRouter.Instance.Route(packet, this);
        }
    }

    public override void Accept(Packet packet)
    {
    }
}
