using System.Diagnostics;
using Core.Contracts;
using Core.Devices;
using Core.Managers;

namespace Core.Handlers;

public class BroadcastHandler : IHandler
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

        if (packet.DirectionForward)
        {
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
        }
        else
        {
            var hdata = (HData?)packet.HandlerData ?? throw new Exception("NULL TRACE");
            var p = packet.RemakeForNextHop(hdata.Trace[^1]);
            p.HandlerData = new HData() { Trace = hdata.Trace[..^1] };
        }


        packet.Terminate();
    }
}
