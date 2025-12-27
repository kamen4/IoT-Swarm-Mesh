using Core.Devices;
using Core.Statistics;

namespace Core.Services.Handlers;

/// <summary>
/// Широковещательный обработчик - отправляет пакет всем связанным соседям (flooding)
/// </summary>
public class BroadcastHandler : IPacketHandler
{
    public string Name => "Широковещательный";
    public string Description => "Отправляет пакет всем связанным соседям (flooding)";

    public class TraceData
    {
        public HashSet<Guid> Visited { get; set; } = [];
    }

    public void Handle(Device device, Packet packet, IPacketService packetService, IDeviceService deviceService, IStatisticsService statistics)
    {
        // Deliver to current device if it's the receiver or broadcast
        if (packet.Receiver is null)
        {
            device.AcceptPacket(packet);
        }
        else if (packet.Receiver.Id == device.Id)
        {
            device.AcceptPacket(packet);
            var deliveryTime = DateTime.UtcNow - packet.OriginalCreatedOn;
            statistics.RecordPacketDelivered(packet, deliveryTime);
            return; // Packet delivered, don't forward
        }

        if (packet.TTL <= 0)
        {
            statistics.RecordPacketDropped(packet, "TTL expired");
            return;
        }

        packet.HandlerData ??= new TraceData();
        var data = (TraceData)packet.HandlerData;
        data.Visited.Add(device.Id);

        // Отправляем всем СВЯЗАННЫМ соседям
        foreach (var neighbor in device.Connections)
        {
            if (!data.Visited.Contains(neighbor.Id))
            {
                var newPacket = packet.RemakeForNextHop(neighbor);
                newPacket.HandlerData = new TraceData { Visited = [.. data.Visited] };
                packetService.RegisterPacket(newPacket);
            }
        }
    }
}
