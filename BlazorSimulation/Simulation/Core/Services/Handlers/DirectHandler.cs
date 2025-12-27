using Core.Devices;
using Core.Statistics;

namespace Core.Services.Handlers;

/// <summary>
/// Прямой обработчик - пытается отправить напрямую по связям
/// </summary>
public class DirectHandler : IPacketHandler
{
    public string Name => "Прямой";
    public string Description => "Отправляет напрямую если есть связь, иначе ищет ближайшего соседа";

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

        // Ищем получателя среди связанных устройств
        var target = device.Connections.FirstOrDefault(d => d.Id == packet.Receiver.Id);
        
        if (target is not null)
        {
            // Отправляем напрямую
            var newPacket = packet.RemakeForNextHop(target);
            packetService.RegisterPacket(newPacket);
        }
        else if (device.Connections.Count > 0)
        {
            // Ищем ближайшего к цели соседа среди связанных
            var receiverPos = packet.Receiver.Pos;
            var closest = device.Connections
                .OrderBy(d => System.Numerics.Vector2.Distance(d.Pos, receiverPos))
                .First();
            
            var newPacket = packet.RemakeForNextHop(closest);
            packetService.RegisterPacket(newPacket);
        }
        else
        {
            statistics.RecordPacketDropped(packet, "No connected neighbors");
        }
    }
}
