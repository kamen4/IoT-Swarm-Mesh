using Core.Contracts;
using Core.Managers;

namespace Core;

public class BroadcastBacktrackPH : IPacketHandler
{
    public class HData
    {
        public List<Device> Trace { get; set; } = [];
    }

    public void Handle(Device device, Packet packet)
    {
        if (packet.Receiver == device)
        {
            device.AcceptPacket(packet);
            packet.Terminate();
            return;
        }

        packet.HandlerData ??= new HData();
        var data = (HData)packet.HandlerData;
        data.Trace.Add(device);

        var vis = DeviceManager.GetVisibilitiesForDevice(device);
        foreach (var neigh in vis)
        {
            if (!data.Trace.Contains(neigh))
            {
                var p = packet.RemakeForNextHop(neigh);
                p.HandlerData = new HData() { Trace = [.. data.Trace] };
            }
        }

        packet.Terminate();
    }
}
