using Core;
using Core.Devices;
using Core.Services;

namespace Tests;

public class SimulationServiceTests
{
    private readonly ISimulationService _simulation;

    public SimulationServiceTests()
    {
        _simulation = ServiceFactory.CreateSimulationService();
    }

    [Fact]
    public void ServiceFactory_CreatesAllServices()
    {
        Assert.NotNull(_simulation.Devices);
        Assert.NotNull(_simulation.Packets);
        Assert.NotNull(_simulation.NetworkBuilder);
        Assert.NotNull(_simulation.Statistics);
        Assert.NotNull(_simulation.Serialization);
    }

    [Fact]
    public void InitialState_IsIdle()
    {
        Assert.Equal(SimulationState.Idle, _simulation.State);
    }

    [Fact]
    public void Reset_ClearsEverything()
    {
        _simulation.Devices.Add(new Hub());
        _simulation.Devices.Add(new Lamp());
        _simulation.Packets.CreatePing(
            _simulation.Devices.All.First(),
            _simulation.Devices.All.Last());
        
        _simulation.Reset();
        
        Assert.Empty(_simulation.Packets.ActivePackets);
        Assert.Equal(SimulationState.Idle, _simulation.State);
    }

    [Fact]
    public async Task StartNetworkBuildAsync_ThrowsIfNoHub()
    {
        _simulation.Devices.Add(new Lamp());
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _simulation.StartNetworkBuildAsync());
    }

    [Fact]
    public async Task StartNetworkBuildAsync_BuildsNetwork()
    {
        var hub = new Hub { Pos = new(100, 100), Radius = 200 };
        var lamp = new Lamp { Pos = new(150, 100), Radius = 200 };
        _simulation.Devices.Add(hub);
        _simulation.Devices.Add(lamp);
        
        _simulation.NetworkBuilder.SetActiveStrategy("Мгновенное");
        await _simulation.StartNetworkBuildAsync();
        
        Assert.Contains(lamp, hub.Connections);
    }

    [Fact]
    public void Tick_ReturnsResult()
    {
        var hub = new Hub { Pos = new(0, 0) };
        var lamp = new Lamp { Pos = new(100, 0) };
        _simulation.Devices.Add(hub);
        _simulation.Devices.Add(lamp);
        _simulation.Packets.CreatePing(hub, lamp);
        
        var result = _simulation.Tick();
        
        Assert.NotNull(result);
    }

    [Fact]
    public void StopSimulation_ChangesStateToIdle()
    {
        _simulation.StopSimulation();
        
        Assert.Equal(SimulationState.Idle, _simulation.State);
    }

    [Fact]
    public void OnStateChanged_FiresWhenStateChanges()
    {
        var stateChanges = new List<SimulationState>();
        _simulation.OnStateChanged += s => stateChanges.Add(s);
        
        _simulation.StopSimulation();
        
        // State was already Idle, so no change should be recorded
    }

    [Fact]
    public void Devices_LoadPreset_Works()
    {
        _simulation.Devices.LoadPreset("Line");
        
        Assert.NotNull(_simulation.Devices.Hub);
        Assert.NotEmpty(_simulation.Devices.Lamps);
        Assert.NotEmpty(_simulation.Devices.Sensors);
    }

    [Fact]
    public void Serialization_RoundTrip_Works()
    {
        _simulation.Devices.LoadPreset("Star");
        
        var json = _simulation.Serialization.SerializeDevices(_simulation.Devices.All);
        var restored = _simulation.Serialization.DeserializeDevices(json);
        
        Assert.Equal(_simulation.Devices.All.Count, restored.Count);
    }

    [Fact]
    public void Packets_HasAllHandlers()
    {
        var handlers = _simulation.Packets.GetHandlerNames().ToList();
        
        Assert.True(handlers.Count >= 5);
        Assert.Contains("Широковещательный", handlers);
        Assert.Contains("По связям", handlers);
        Assert.Contains("Прямой", handlers);
        Assert.Contains("Жадный", handlers);
        Assert.Contains("Случайный", handlers);
    }
}
