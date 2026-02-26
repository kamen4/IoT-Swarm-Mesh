using Engine.Devices;
using Engine.Packets;
using System;

namespace Engine.Core;
/// <summary>
/// q
/// </summary>
public class SimulationEngine
{
    public const int VISIBILITY_DISTANCE = 200;
    /// <summary>
    /// q
    /// </summary>
    public static SimulationEngine Instance { get; } = new();

    private DateTime _lastTickTime = DateTime.Now;
    public long TickCount { get; private set; } = 0;
    public Device? Hub { get; private set; }

    private readonly PriorityQueue<Packet, long> _packets = new();
    private readonly List<Device> _devices = [];

    public event EventHandler? TickEvent;

    /// <summary>
    /// q
    /// </summary>
    /// <returns>q</returns>
    public (long tick, double dt) Tick()
    {
        ++TickCount;
        var dt = UpdateTime();
        TickEvent?.Invoke(this, EventArgs.Empty);

        TickPackets();
        for (int i = 1; i < 100_000_000; i++) _ = i * i / i * i / i;

        return (TickCount, dt);
    }

    private void TickPackets()
    {
        while (_packets.Count > 0 && _packets.Peek().ArrivalTick >= TickCount)
        {
            var p = _packets.Dequeue();
            if (--p.TTL > 0)
            {
                p.NextHop.Recieve(p);
            }
            else
            {
                //LOSS
            }
        }
    }

    public void RegisterPacket(Packet packet)
    {
        var arivalTick = TickCount + packet.TicksToTravel;
        packet.ArrivalTick = arivalTick;
        _packets.Enqueue(packet, -arivalTick);
    }

    public void RegisterDevice(Device device)
    {
        if (device is HubDevice)
        {
            Hub = device;
        }

        _devices.Add(device);
    }

    private double UpdateTime()
    {
        var ret = ( DateTime.Now - _lastTickTime ).TotalMilliseconds;
        _lastTickTime = DateTime.Now;
        return ret;
    }

    public IEnumerable<Device> GetVisibleDevicesFor(Device d)
    {
        foreach (var device in _devices)
        {
            if (device.Id == d.Id)
            {
                continue;
            }
            if ((device.Position - d.Position).Length() <= VISIBILITY_DISTANCE)
            {
                yield return device;
            }
        }
    }
}
