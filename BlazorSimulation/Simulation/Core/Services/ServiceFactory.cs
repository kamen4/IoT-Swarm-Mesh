using Core.Services.Handlers;
using Core.Services.Strategies;
using Core.Statistics;

namespace Core.Services;

/// <summary>
/// Фабрика для создания и конфигурации сервисов симуляции
/// </summary>
public static class ServiceFactory
{
    /// <summary>
    /// Создает полностью настроенный SimulationService со всеми зависимостями
    /// </summary>
    public static ISimulationService CreateSimulationService()
    {
        var deviceService = new DeviceService();
        var statisticsService = new StatisticsService();
        var serializationService = new SerializationService();
        
        var handlers = new List<IPacketHandler>
        {
            new BroadcastHandler(),
            new ConnectionBasedHandler(),
            new DirectHandler(),
            new GreedyHandler(),
            new RandomHandler()
        };
        
        var packetService = new PacketService(deviceService, statisticsService, handlers);
        
        var strategies = new List<INetworkBuildStrategy>
        {
            new InstantBuildStrategy(),
            new SproutBuildStrategy()
        };
        
        var networkBuilderService = new NetworkBuilderService(deviceService, packetService, strategies);
        
        // Создаём NetworkBuildHandler с доступом к активной стратегии
        var networkBuildHandler = new NetworkBuildHandler(() => networkBuilderService.ActiveStrategy);
        packetService.SetNetworkBuildHandler(networkBuildHandler);
        
        return new SimulationService(
            deviceService,
            packetService,
            networkBuilderService,
            statisticsService,
            serializationService
        );
    }

    /// <summary>
    /// Создает сервис с кастомными обработчиками и стратегиями
    /// </summary>
    public static ISimulationService CreateSimulationService(
        IEnumerable<IPacketHandler> handlers,
        IEnumerable<INetworkBuildStrategy> strategies)
    {
        var deviceService = new DeviceService();
        var statisticsService = new StatisticsService();
        var serializationService = new SerializationService();
        var packetService = new PacketService(deviceService, statisticsService, handlers);
        var networkBuilderService = new NetworkBuilderService(deviceService, packetService, strategies);
        
        var networkBuildHandler = new NetworkBuildHandler(() => networkBuilderService.ActiveStrategy);
        packetService.SetNetworkBuildHandler(networkBuildHandler);
        
        return new SimulationService(
            deviceService,
            packetService,
            networkBuilderService,
            statisticsService,
            serializationService
        );
    }
}
