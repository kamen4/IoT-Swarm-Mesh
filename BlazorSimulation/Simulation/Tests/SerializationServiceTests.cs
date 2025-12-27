using Core.Devices;
using Core.Services;

namespace Tests;

public class SerializationServiceTests
{
    private readonly ISerializationService _serialization;

    public SerializationServiceTests()
    {
        _serialization = new SerializationService();
    }

    [Fact]
    public void SerializeDevices_SerializesToJson()
    {
        var devices = new List<Device>
        {
            new Hub { Name = "H", Pos = new(100, 100), Radius = 200 },
            new Lamp { Name = "L1", Pos = new(200, 200), IsLampOn = true },
            new Sensor { Name = "S1", Pos = new(300, 300) }
        };
        
        var json = _serialization.SerializeDevices(devices);
        
        Assert.Contains("\"name\": \"H\"", json);
        Assert.Contains("\"type\": \"Hub\"", json);
        Assert.Contains("\"type\": \"Lamp\"", json);
        Assert.Contains("\"type\": \"Sensor\"", json);
    }

    [Fact]
    public void DeserializeDevices_RestoresDevices()
    {
        var original = new List<Device>
        {
            new Hub { Name = "H", Pos = new(100, 100), Radius = 200 },
            new Lamp { Name = "L1", Pos = new(200, 200), IsLampOn = false },
            new Sensor { Name = "S1", Pos = new(300, 300), Battery = 0.5 }
        };
        
        var json = _serialization.SerializeDevices(original);
        var restored = _serialization.DeserializeDevices(json);
        
        Assert.Equal(3, restored.Count);
        
        var hub = restored.OfType<Hub>().Single();
        Assert.Equal("H", hub.Name);
        Assert.Equal(100, hub.Pos.X);
        Assert.Equal(200, hub.Radius);
        
        var lamp = restored.OfType<Lamp>().Single();
        Assert.Equal("L1", lamp.Name);
        Assert.False(lamp.IsLampOn);
        
        var sensor = restored.OfType<Sensor>().Single();
        Assert.Equal("S1", sensor.Name);
        Assert.Equal(0.5, sensor.Battery);
    }

    [Fact]
    public void SerializeState_IncludesConnections()
    {
        var snapshot = new SimulationSnapshot
        {
            Devices = 
            [
                new DeviceDto { Id = Guid.NewGuid(), Name = "H", Type = "Hub" },
                new DeviceDto { Id = Guid.NewGuid(), Name = "L1", Type = "Lamp" }
            ],
            Connections = 
            [
                new ConnectionDto { DeviceId1 = Guid.NewGuid(), DeviceId2 = Guid.NewGuid() }
            ],
            Description = "Test snapshot"
        };
        
        var json = _serialization.SerializeState(snapshot);
        
        Assert.Contains("\"description\": \"Test snapshot\"", json);
        Assert.Contains("connections", json);
    }

    [Fact]
    public void DeserializeState_RestoresSnapshot()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var snapshot = new SimulationSnapshot
        {
            Devices = 
            [
                new DeviceDto { Id = id1, Name = "H", Type = "Hub" },
                new DeviceDto { Id = id2, Name = "L1", Type = "Lamp" }
            ],
            Connections = [new ConnectionDto { DeviceId1 = id1, DeviceId2 = id2 }],
            Description = "Test"
        };
        
        var json = _serialization.SerializeState(snapshot);
        var restored = _serialization.DeserializeState(json);
        
        Assert.Equal(2, restored.Devices.Count);
        Assert.Single(restored.Connections);
        Assert.Equal("Test", restored.Description);
    }

    [Fact]
    public void SavePreset_And_LoadPreset_Works()
    {
        var devices = new List<Device>
        {
            new Hub { Name = "TestHub" },
            new Lamp { Name = "TestLamp" }
        };
        
        _serialization.SavePreset("MyPreset", devices);
        var loaded = _serialization.LoadPreset("MyPreset");
        
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Count);
    }

    [Fact]
    public void LoadPreset_ReturnsNullForUnknownPreset()
    {
        var result = _serialization.LoadPreset("NonExistent");
        
        Assert.Null(result);
    }

    [Fact]
    public void GetSavedPresets_ReturnsPresetNames()
    {
        _serialization.SavePreset("Preset1", [new Hub()]);
        _serialization.SavePreset("Preset2", [new Lamp()]);
        
        var presets = _serialization.GetSavedPresets().ToList();
        
        Assert.Contains("Preset1", presets);
        Assert.Contains("Preset2", presets);
    }

    [Fact]
    public void BatteryDrainRate_IsSerializedAndDeserialized()
    {
        var devices = new List<Device>
        {
            new Lamp { Name = "L", BatteryDrainRate = 2.5 }
        };
        
        var json = _serialization.SerializeDevices(devices);
        var restored = _serialization.DeserializeDevices(json);
        
        Assert.Equal(2.5, restored[0].BatteryDrainRate);
    }
}
