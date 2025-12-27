using Core.Devices;
using Core.Statistics;

namespace Core.Services.Handlers;

/// <summary>
/// Обработчик пакетов построения сети - передаёт пакеты активной стратегии
/// </summary>
public class NetworkBuildHandler : IPacketHandler
{
    private readonly Func<INetworkBuildStrategy> _getActiveStrategy;

    public NetworkBuildHandler(Func<INetworkBuildStrategy> getActiveStrategy)
    {
        _getActiveStrategy = getActiveStrategy;
    }

    public string Name => "NetworkBuild";
    public string Description => "Обработчик пакетов построения сети";

    public void Handle(Device device, Packet packet, IPacketService packetService, IDeviceService deviceService, IStatisticsService statistics)
    {
        if (packet.PacketType != PacketType.NetworkBuild)
        {
            return;
        }

        // Передаём пакет активной стратегии построения
        var strategy = _getActiveStrategy();
        strategy.AcceptBuildPacket(device, packet, deviceService);
        
        // Записываем доставку
        var deliveryTime = DateTime.UtcNow - packet.OriginalCreatedOn;
        statistics.RecordPacketDelivered(packet, deliveryTime);
    }
}
