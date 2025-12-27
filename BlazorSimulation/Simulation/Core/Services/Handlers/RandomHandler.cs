using Core.Devices;
using Core.Statistics;

namespace Core.Services.Handlers;

/// <summary>
/// Случайный обработчик - выбирает случайного соседа из связанных для пересылки
/// </summary>
public class RandomHandler : IPacketHandler
{
    private readonly Random _random = new();
    
    public string Name => "Случайный";
    public string Description => "Выбирает случайного соседа для пересылки пакета";

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

        packet.HandlerData ??= new VisitedData();
        var data = (VisitedData)packet.HandlerData;
        data.Visited.Add(device.Id);

        // Получаем связанных соседей которых еще не посещали
        var candidates = device.Connections
            .Where(d => !data.Visited.Contains(d.Id))
            .ToList();

        if (candidates.Count == 0)
        {
            // Нет новых соседей - пробуем всех связанных
            candidates = device.Connections.ToList();
        }

        if (candidates.Count == 0)
        {
            statistics.RecordPacketDropped(packet, "No connected neighbors");
            return;
        }

        // Выбираем случайного
        var next = candidates[_random.Next(candidates.Count)];
        var newPacket = packet.RemakeForNextHop(next);
        newPacket.HandlerData = new VisitedData { Visited = [.. data.Visited] };
        packetService.RegisterPacket(newPacket);
    }
}
