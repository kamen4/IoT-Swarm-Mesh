using Core.Devices;
using Core.Services;
using System.Numerics;

namespace Tests;

public class DeviceServiceTests
{
    private readonly IDeviceService _deviceService;

    public DeviceServiceTests()
    {
        _deviceService = new DeviceService();
    }

    [Fact]
    public void Add_AddsDeviceToCollection()
    {
        var device = new Lamp { Name = "Test" };
        
        _deviceService.Add(device);
        
        Assert.Contains(device, _deviceService.All);
    }

    [Fact]
    public void Remove_RemovesDeviceFromCollection()
    {
        var device = new Lamp { Name = "Test" };
        _deviceService.Add(device);
        
        _deviceService.Remove(device);
        
        Assert.DoesNotContain(device, _deviceService.All);
    }

    [Fact]
    public void Remove_RemovesDeviceFromConnections()
    {
        var lamp = new Lamp { Name = "Lamp", Pos = new(0, 0), Radius = 100 };
        var sensor = new Sensor { Name = "Sensor", Pos = new(50, 0), Radius = 100 };
        lamp.Connections.Add(sensor);
        sensor.Connections.Add(lamp);
        _deviceService.Add(lamp);
        _deviceService.Add(sensor);
        
        _deviceService.Remove(lamp);
        
        Assert.DoesNotContain(lamp, sensor.Connections);
    }

    [Fact]
    public void Clear_RemovesAllDevices()
    {
        _deviceService.Add(new Lamp());
        _deviceService.Add(new Sensor());
        
        _deviceService.Clear();
        
        Assert.Empty(_deviceService.All);
    }

    [Fact]
    public void GetById_ReturnsCorrectDevice()
    {
        var device = new Lamp { Name = "Test" };
        _deviceService.Add(device);
        
        var result = _deviceService.GetById(device.Id);
        
        Assert.Equal(device, result);
    }

    [Fact]
    public void GetById_ReturnsNullForUnknownId()
    {
        var result = _deviceService.GetById(Guid.NewGuid());
        
        Assert.Null(result);
    }

    [Fact]
    public void GetAtPosition_ReturnsDeviceAtPosition()
    {
        var device = new Lamp { Pos = new(100, 100) };
        _deviceService.Add(device);
        
        var result = _deviceService.GetAtPosition(new(105, 100));
        
        Assert.Equal(device, result);
    }

    [Fact]
    public void GetAtPosition_ReturnsNullIfNoDeviceAtPosition()
    {
        var device = new Lamp { Pos = new(100, 100) };
        _deviceService.Add(device);
        
        var result = _deviceService.GetAtPosition(new(500, 500));
        
        Assert.Null(result);
    }

    [Fact]
    public void AreDevicesVisible_ReturnsTrueWhenInRange()
    {
        var d1 = new Lamp { Pos = new(0, 0), Radius = 100 };
        var d2 = new Sensor { Pos = new(50, 0), Radius = 100 };
        
        var result = _deviceService.AreDevicesVisible(d1, d2);
        
        Assert.True(result);
    }

    [Fact]
    public void AreDevicesVisible_ReturnsFalseWhenOutOfRange()
    {
        var d1 = new Lamp { Pos = new(0, 0), Radius = 50 };
        var d2 = new Sensor { Pos = new(200, 0), Radius = 50 };
        
        var result = _deviceService.AreDevicesVisible(d1, d2);
        
        Assert.False(result);
    }

    [Fact]
    public void GetVisibleDevices_ReturnsOnlyVisibleDevices()
    {
        var center = new Hub { Pos = new(100, 100), Radius = 100 };
        var near = new Lamp { Pos = new(150, 100), Radius = 100 };
        var far = new Sensor { Pos = new(500, 500), Radius = 50 };
        
        _deviceService.Add(center);
        _deviceService.Add(near);
        _deviceService.Add(far);
        
        var visible = _deviceService.GetVisibleDevices(center).ToList();
        
        Assert.Contains(near, visible);
        Assert.DoesNotContain(far, visible);
    }

    [Fact]
    public void Hub_ReturnsHubDevice()
    {
        var hub = new Hub { Name = "Hub" };
        _deviceService.Add(hub);
        _deviceService.Add(new Lamp());
        
        Assert.Equal(hub, _deviceService.Hub);
    }

    [Fact]
    public void Lamps_ReturnsOnlyLamps()
    {
        _deviceService.Add(new Hub());
        _deviceService.Add(new Lamp { Name = "L1" });
        _deviceService.Add(new Lamp { Name = "L2" });
        _deviceService.Add(new Sensor());
        
        var lamps = _deviceService.Lamps.ToList();
        
        Assert.Equal(2, lamps.Count);
        Assert.All(lamps, l => Assert.IsType<Lamp>(l));
    }

    [Fact]
    public void Sensors_ReturnsOnlySensors()
    {
        _deviceService.Add(new Hub());
        _deviceService.Add(new Lamp());
        _deviceService.Add(new Sensor { Name = "S1" });
        _deviceService.Add(new Sensor { Name = "S2" });
        
        var sensors = _deviceService.Sensors.ToList();
        
        Assert.Equal(2, sensors.Count);
        Assert.All(sensors, s => Assert.IsType<Sensor>(s));
    }

    [Fact]
    public void LoadPreset_LoadsDevicesFromPreset()
    {
        _deviceService.LoadPreset("Line");
        
        Assert.NotEmpty(_deviceService.All);
        Assert.NotNull(_deviceService.Hub);
    }

    [Fact]
    public void GetPresetNames_ReturnsAvailablePresets()
    {
        var presets = _deviceService.GetPresetNames().ToList();
        
        Assert.Contains("Diamonds", presets);
        Assert.Contains("Line", presets);
        Assert.Contains("Star", presets);
    }
}
