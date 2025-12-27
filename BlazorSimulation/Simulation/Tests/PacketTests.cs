using Core;
using Core.Devices;
using Core.Services;

namespace Tests;

public class PacketTests
{
    [Fact]
    public void Packet_HasUniqueId()
    {
        var p1 = new Packet();
        var p2 = new Packet();
        
        Assert.NotEqual(p1.Id, p2.Id);
    }

    [Fact]
    public void Packet_Constructor_SetsSenderAndReceiver()
    {
        var sender = new Hub();
        var receiver = new Lamp();
        
        var packet = new Packet(sender, receiver);
        
        Assert.Equal(sender, packet.Sender);
        Assert.Equal(receiver, packet.Receiver);
        Assert.Equal(sender, packet.CurrentHop);
        // NextHop изначально = sender (пакет ещё не начал движение)
        Assert.Equal(sender, packet.NextHop);
        Assert.True(packet.IsInitial);
    }

    [Fact]
    public void Packet_Constructor_WithNullReceiver_SetsNextHopToSender()
    {
        var sender = new Hub();
        
        var packet = new Packet(sender, null);
        
        Assert.Equal(sender, packet.Sender);
        Assert.Null(packet.Receiver);
        Assert.Equal(sender, packet.CurrentHop);
        Assert.Equal(sender, packet.NextHop);
    }

    [Fact]
    public void Packet_RemakeForNextHop_CreatesNewPacket()
    {
        var hub = new Hub();
        var lamp = new Lamp();
        var sensor = new Sensor();
        
        var original = new Packet(hub, lamp)
        {
            PacketType = PacketType.Data,
            Payload = [1, 2, 3],
            TTL = 64
        };
        
        var remade = original.RemakeForNextHop(sensor);
        
        Assert.NotEqual(original.Id, remade.Id);
        Assert.Equal(original.IdempotencyId, remade.IdempotencyId);
        Assert.Equal(original.Sender, remade.Sender);
        Assert.Equal(original.Receiver, remade.Receiver);
        Assert.Equal(original.PacketType, remade.PacketType);
        Assert.Equal(sensor, remade.NextHop);
        Assert.Equal(63, remade.TTL);
        Assert.Equal(1, remade.HopCount);
        Assert.False(remade.IsInitial);
    }

    [Fact]
    public void Packet_TTL_DecrementsOnHop()
    {
        var hub = new Hub();
        var lamp = new Lamp();
        
        var p1 = new Packet(hub, lamp) { TTL = 10 };
        var p2 = p1.RemakeForNextHop(lamp);
        var p3 = p2.RemakeForNextHop(hub);
        
        Assert.Equal(9, p2.TTL);
        Assert.Equal(8, p3.TTL);
    }

    [Fact]
    public void Packet_HopCount_IncrementsOnHop()
    {
        var hub = new Hub();
        var lamp = new Lamp();
        
        var p1 = new Packet(hub, lamp);
        var p2 = p1.RemakeForNextHop(lamp);
        var p3 = p2.RemakeForNextHop(hub);
        
        Assert.Equal(0, p1.HopCount);
        Assert.Equal(1, p2.HopCount);
        Assert.Equal(2, p3.HopCount);
    }

    [Fact]
    public void Packet_Equals_ComparesById()
    {
        var p1 = new Packet();
        var p2 = new Packet();
        
        Assert.NotEqual(p1, p2);
        Assert.Equal(p1, p1);
    }

    [Fact]
    public void Packet_DefaultTTL_Is64()
    {
        var packet = new Packet();
        
        Assert.Equal(64, packet.TTL);
    }

    [Fact]
    public void Packet_CreatedOn_IsSetAutomatically()
    {
        var before = DateTime.UtcNow;
        var packet = new Packet();
        var after = DateTime.UtcNow;
        
        Assert.InRange(packet.CreatedOn, before, after);
    }
}
