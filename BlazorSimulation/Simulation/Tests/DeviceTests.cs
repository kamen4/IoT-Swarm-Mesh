using Core;
using Core.Devices;
using Core.Services;

namespace Tests;

public class DeviceTests
{
    [Fact]
    public void Device_HasUniqueId()
    {
        var d1 = new Lamp();
        var d2 = new Lamp();
        
        Assert.NotEqual(d1.Id, d2.Id);
    }

    [Fact]
    public void Device_Clone_CreatesNewId()
    {
        var original = new Lamp { Name = "Test", Battery = 0.5 };
        
        var clone = (Lamp)original.Clone();
        
        Assert.NotEqual(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Battery, clone.Battery);
    }

    [Fact]
    public void Device_Clone_ClearsConnections()
    {
        var lamp = new Lamp();
        var sensor = new Sensor();
        lamp.Connections.Add(sensor);
        
        var clone = (Lamp)lamp.Clone();
        
        Assert.Empty(clone.Connections);
    }

    [Fact]
    public void Hub_HasACPower()
    {
        var hub = new Hub();
        
        Assert.Equal(Device.PowerType.AC, hub.DevicePowerType);
    }

    [Fact]
    public void Lamp_AcceptPacket_ProcessesLampCommand()
    {
        var lamp = new Lamp { IsLampOn = true };
        var packet = new Packet(new Hub(), lamp)
        {
            PacketType = PacketType.LampCommand,
            Payload = [0] // Turn off
        };
        
        lamp.AcceptPacket(packet);
        
        Assert.False(lamp.IsLampOn);
    }

    [Fact]
    public void Lamp_AcceptPacket_TurnsOn()
    {
        var lamp = new Lamp { IsLampOn = false };
        var packet = new Packet(new Hub(), lamp)
        {
            PacketType = PacketType.LampCommand,
            Payload = [1] // Turn on
        };
        
        lamp.AcceptPacket(packet);
        
        Assert.True(lamp.IsLampOn);
    }

    [Fact]
    public void Lamp_Color_ChangesBasedOnState()
    {
        var lamp = new Lamp();
        
        lamp.IsLampOn = true;
        var onColor = lamp.Color;
        
        lamp.IsLampOn = false;
        var offColor = lamp.Color;
        
        Assert.NotEqual(onColor, offColor);
    }

    [Fact]
    public void Sensor_GenData_ReturnsValue()
    {
        var sensor = new Sensor();
        
        var value = sensor.GenData();
        
        Assert.InRange(value, 0, 100);
        Assert.Equal(value, sensor.LastValue);
    }

    [Fact]
    public void Sensor_GenData_IsDeterministic()
    {
        var id = Guid.NewGuid();
        var s1 = new Sensor(id);
        var s2 = new Sensor(id);
        
        Assert.Equal(s1.GenData(), s2.GenData());
    }

    [Fact]
    public void Device_AcceptPacket_IgnoresDuplicates()
    {
        var lamp = new Lamp { IsLampOn = true };
        var packet = new Packet(new Hub(), lamp)
        {
            PacketType = PacketType.LampCommand,
            Payload = [0]
        };
        
        lamp.AcceptPacket(packet);
        Assert.False(lamp.IsLampOn);
        
        // Turn back on manually
        lamp.IsLampOn = true;
        
        // Same packet should be ignored
        lamp.AcceptPacket(packet);
        Assert.True(lamp.IsLampOn);
    }

    [Fact]
    public void Device_ClearProcessedPackets_AllowsReprocessing()
    {
        var lamp = new Lamp { IsLampOn = true };
        var packet = new Packet(new Hub(), lamp)
        {
            PacketType = PacketType.LampCommand,
            Payload = [0]
        };
        
        lamp.AcceptPacket(packet);
        lamp.IsLampOn = true;
        
        lamp.ClearProcessedPackets();
        lamp.AcceptPacket(packet);
        
        Assert.False(lamp.IsLampOn);
    }

    [Fact]
    public void Device_BatteryDrainRate_DefaultIsOne()
    {
        var lamp = new Lamp();
        
        Assert.Equal(1.0, lamp.BatteryDrainRate);
    }

    [Fact]
    public void Device_Equals_ComparesById()
    {
        var id = Guid.NewGuid();
        var d1 = new Lamp(id);
        var d2 = new Lamp(id);
        
        Assert.Equal(d1, d2);
    }
}
