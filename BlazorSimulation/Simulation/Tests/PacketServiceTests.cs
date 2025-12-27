using Core;
using Core.Devices;
using Core.Services;

namespace Tests;

public class PacketServiceTests
{
    private readonly IDeviceService _deviceService;
    private readonly IPacketService _packetService;

    public PacketServiceTests()
    {
        var sim = ServiceFactory.CreateSimulationService();
        _deviceService = sim.Devices;
        _packetService = sim.Packets;
    }

    [Fact]
    public void CreatePing_CreatesPacketWithCorrectType()
    {
        var sender = new Hub();
        var receiver = new Lamp();
        _deviceService.Add(sender);
        _deviceService.Add(receiver);

        var packet = _packetService.CreatePing(sender, receiver);

        Assert.Equal(PacketType.Ping, packet.PacketType);
        Assert.Equal(sender, packet.Sender);
        Assert.Equal(receiver, packet.Receiver);
    }

    [Fact]
    public void CreateLampCommand_SetsPayload()
    {
        var hub = new Hub();
        var lamp = new Lamp();
        _deviceService.Add(hub);
        _deviceService.Add(lamp);

        var packet = _packetService.CreateLampCommand(hub, lamp, true);

        Assert.Equal(PacketType.LampCommand, packet.PacketType);
        Assert.NotNull(packet.Payload);
        Assert.Equal(1, packet.Payload[0]);
    }

    [Fact]
    public void CreateSensorData_SetsPayload()
    {
        var sensor = new Sensor();
        var hub = new Hub();
        _deviceService.Add(sensor);
        _deviceService.Add(hub);

        var packet = _packetService.CreateSensorData(sensor, hub, 42.5);

        Assert.Equal(PacketType.SensorData, packet.PacketType);
        Assert.NotNull(packet.Payload);
        Assert.Equal(42.5, BitConverter.ToDouble(packet.Payload));
    }

    [Fact]
    public void RegisterPacket_AddsToActivePackets()
    {
        var hub = new Hub();
        var lamp = new Lamp();
        _deviceService.Add(hub);
        _deviceService.Add(lamp);

        var packet = _packetService.CreatePing(hub, lamp);

        Assert.Contains(packet, _packetService.ActivePackets);
    }

    [Fact]
    public void TerminatePacket_RemovesFromActivePackets()
    {
        var hub = new Hub();
        var lamp = new Lamp();
        _deviceService.Add(hub);
        _deviceService.Add(lamp);

        var packet = _packetService.CreatePing(hub, lamp);
        _packetService.TerminatePacket(packet);

        Assert.DoesNotContain(packet, _packetService.ActivePackets);
    }

    [Fact]
    public void Clear_RemovesAllPackets()
    {
        var hub = new Hub();
        var lamp = new Lamp();
        _deviceService.Add(hub);
        _deviceService.Add(lamp);

        _packetService.CreatePing(hub, lamp);
        _packetService.CreatePing(hub, lamp);
        _packetService.Clear();

        Assert.Empty(_packetService.ActivePackets);
    }

    [Fact]
    public void SetActiveHandler_ChangesHandler()
    {
        var handlerNames = _packetService.GetHandlerNames().ToList();
        Assert.True(handlerNames.Count >= 2);

        var firstHandler = _packetService.ActiveHandler;
        _packetService.SetActiveHandler(handlerNames[1]);

        Assert.NotEqual(firstHandler.Name, _packetService.ActiveHandler.Name);
    }

    [Fact]
    public void SetActiveHandler_ThrowsOnInvalidName()
    {
        Assert.Throws<KeyNotFoundException>(() => 
            _packetService.SetActiveHandler("NonExistentHandler"));
    }

    [Fact]
    public void GetHandlerNames_ReturnsAllHandlers()
    {
        var names = _packetService.GetHandlerNames().ToList();

        Assert.Contains("Широковещательный", names);
        Assert.Contains("По связям", names);
        Assert.Contains("Прямой", names);
        Assert.Contains("Жадный", names);
        Assert.Contains("Случайный", names);
    }
}
