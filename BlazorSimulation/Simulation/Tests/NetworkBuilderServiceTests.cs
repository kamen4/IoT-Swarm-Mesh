using Core.Devices;
using Core.Services;
using Core.Services.Handlers;
using Core.Services.Strategies;
using Core.Statistics;

namespace Tests;

public class NetworkBuilderServiceTests
{
    private readonly IDeviceService _deviceService;
    private readonly IPacketService _packetService;
    private readonly INetworkBuilderService _networkBuilder;

    public NetworkBuilderServiceTests()
    {
        _deviceService = new DeviceService();
        var statisticsService = new StatisticsService();
        var handlers = new List<IPacketHandler> { new BroadcastHandler() };
        _packetService = new PacketService(_deviceService, statisticsService, handlers);
        var strategies = new List<INetworkBuildStrategy> 
        { 
            new SproutBuildStrategy(), 
            new InstantBuildStrategy() 
        };
        _networkBuilder = new NetworkBuilderService(_deviceService, _packetService, strategies);
        
        // –егистрируем NetworkBuildHandler
        var networkBuildHandler = new NetworkBuildHandler(() => _networkBuilder.ActiveStrategy);
        _packetService.SetNetworkBuildHandler(networkBuildHandler);
    }

    [Fact]
    public void GetStrategyNames_ReturnsAllStrategies()
    {
        var names = _networkBuilder.GetStrategyNames().ToList();
        
        Assert.Contains("Sprout", names);
        Assert.Contains("ћгновенное", names);
    }

    [Fact]
    public void SetActiveStrategy_ChangesStrategy()
    {
        _networkBuilder.SetActiveStrategy("ћгновенное");
        
        Assert.Equal("ћгновенное", _networkBuilder.ActiveStrategy.Name);
    }

    [Fact]
    public void SetActiveStrategy_ThrowsOnUnknownStrategy()
    {
        Assert.Throws<KeyNotFoundException>(() => 
            _networkBuilder.SetActiveStrategy("Unknown"));
    }

    [Fact]
    public void ClearConnections_RemovesAllConnections()
    {
        var hub = new Hub { Pos = new(100, 100), Radius = 200 };
        var lamp = new Lamp { Pos = new(150, 100), Radius = 200 };
        hub.Connections.Add(lamp);
        lamp.Connections.Add(hub);
        _deviceService.Add(hub);
        _deviceService.Add(lamp);
        
        _networkBuilder.ClearConnections();
        
        Assert.Empty(hub.Connections);
        Assert.Empty(lamp.Connections);
    }

    [Fact]
    public async Task BuildNetworkAsync_InstantStrategy_ConnectsAllVisibleDevices()
    {
        var hub = new Hub { Pos = new(100, 100), Radius = 200 };
        var lamp = new Lamp { Pos = new(150, 100), Radius = 200 };
        var sensor = new Sensor { Pos = new(120, 80), Radius = 200 };
        _deviceService.Add(hub);
        _deviceService.Add(lamp);
        _deviceService.Add(sensor);
        
        _networkBuilder.SetActiveStrategy("ћгновенное");
        await _networkBuilder.BuildNetworkAsync(hub);
        
        Assert.Contains(lamp, hub.Connections);
        Assert.Contains(sensor, hub.Connections);
        Assert.Contains(hub, lamp.Connections);
    }

    [Fact]
    public async Task BuildNetworkAsync_RaisesProgressEvents()
    {
        var hub = new Hub { Pos = new(100, 100), Radius = 200 };
        var lamp = new Lamp { Pos = new(150, 100), Radius = 200 };
        _deviceService.Add(hub);
        _deviceService.Add(lamp);
        
        var progressEvents = new List<NetworkBuildProgress>();
        _networkBuilder.OnProgress += p => progressEvents.Add(p);
        
        _networkBuilder.SetActiveStrategy("ћгновенное");
        await _networkBuilder.BuildNetworkAsync(hub);
        
        Assert.NotEmpty(progressEvents);
        Assert.True(progressEvents.Last().IsComplete);
    }
}
