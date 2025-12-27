using Core.Devices;
using Core.Statistics;
using System.Numerics;

namespace Core.Services.Handlers;

/// <summary>
/// Жадный обработчик - всегда выбирает ближайшего к цели соседа из связанных устройств
/// </summary>
public class GreedyHandler : IPacketHandler
{
    public string Name => "Жадный";
    public string Description => "Всегда выбирает ближайшего к цели соседа (greedy forwarding)";

    public class VisitedData
    {
        public HashSet<Guid> Visited { get; set; } = [];
    }

    public void Handle(Device device, Packet packet, IPacketService packetService, IDeviceService deviceService, IStatisticsService statistics)
    {
        // Доставлено
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

        if (packet.Receiver is null)
        {
            statistics.RecordPacketDropped(packet, "No receiver specified");
            return;
        }

        packet.HandlerData ??= new VisitedData();
        var data = (VisitedData)packet.HandlerData;
        data.Visited.Add(device.Id);

        var targetPos = packet.Receiver.Pos;
        var currentDistance = Vector2.Distance(device.Pos, targetPos);

        // Ищем соседа ближе к цели среди СВЯЗАННЫХ устройств
        var candidates = device.Connections
            .Where(d => !data.Visited.Contains(d.Id))
            .Select(d => new { Device = d, Distance = Vector2.Distance(d.Pos, targetPos) })
            .Where(x => x.Distance < currentDistance) // Только те кто ближе
            .OrderBy(x => x.Distance)
            .ToList();

        if (candidates.Count == 0)
        {
            // Нет соседей ближе - попробуем любого непосещенного связанного
            candidates = device.Connections
                .Where(d => !data.Visited.Contains(d.Id))
                .Select(d => new { Device = d, Distance = Vector2.Distance(d.Pos, targetPos) })
                .OrderBy(x => x.Distance)
                .ToList();
        }

        if (candidates.Count == 0)
        {
            statistics.RecordPacketDropped(packet, "No route available");
            return;
        }

        var next = candidates[0].Device;
        var newPacket = packet.RemakeForNextHop(next);
        newPacket.HandlerData = new VisitedData { Visited = [.. data.Visited] };
        packetService.RegisterPacket(newPacket);
    }
}
