using Engine.Core;
using Engine.Devices;
using Engine.Packets;

namespace Engine.Routers;

public class PacketRouter
{
    public static PacketRouter Instance { get; } = new();
    public void Route(Packet packet, Device device)
    {
        foreach (var d in SimulationEngine.Instance.GetVisibleDevicesFor(device))
        {
            var p = packet.Clone();
            p.NextHop = d;
            SimulationEngine.Instance.RegisterPacket(p);
        }
    }
}