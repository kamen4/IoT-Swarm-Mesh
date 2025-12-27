using Core.Devices;
using Core.Statistics;

namespace Core.Services;

/// <summary>
/// Обработчик пакетов (протокол маршрутизации)
/// </summary>
public interface IPacketHandler
{
    string Name { get; }
    string Description { get; }
    
    void Handle(Device device, Packet packet, IPacketService packetService, IDeviceService deviceService, IStatisticsService statistics);
}
