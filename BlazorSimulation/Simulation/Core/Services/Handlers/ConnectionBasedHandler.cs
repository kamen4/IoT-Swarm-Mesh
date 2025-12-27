using Core.Devices;
using Core.Statistics;

namespace Core.Services.Handlers;

/// <summary>
/// Маршрутизация по связям - использует установленные соединения сети
/// </summary>
public class ConnectionBasedHandler : IPacketHandler
{
    public string Name => "По связям";
    public string Description => "Маршрутизация через установленные соединения сети";

    public class RouteData
    {
        public HashSet<Guid> Visited { get; set; } = [];
    }

    public void Handle(Device device, Packet packet, IPacketService packetService, IDeviceService deviceService, IStatisticsService statistics)
    {
        // Deliver to current device if it's the receiver
        if (packet.Receiver?.Id == device.Id)
        {
            device.AcceptPacket(packet);
            var deliveryTime = DateTime.UtcNow - packet.OriginalCreatedOn;
            statistics.RecordPacketDelivered(packet, deliveryTime);
            return;
        }

        if (packet.TTL <= 0)
        {
            statistics.RecordPacketDropped(packet, "TTL expired");
            return;
        }

        packet.HandlerData ??= new RouteData();
        var data = (RouteData)packet.HandlerData;
        data.Visited.Add(device.Id);

        // Forward to connected devices that haven't been visited
        foreach (var neighbor in device.Connections)
        {
            if (!data.Visited.Contains(neighbor.Id))
            {
                var newPacket = packet.RemakeForNextHop(neighbor);
                newPacket.HandlerData = new RouteData 
                { 
                    Visited = [.. data.Visited] 
                };
                packetService.RegisterPacket(newPacket);
            }
        }
    }
}
