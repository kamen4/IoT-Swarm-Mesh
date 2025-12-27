using Core.Devices;
using Core.Statistics;
using System.Numerics;

namespace Core.Services;

public class PacketService : IPacketService
{
    private readonly HashSet<Packet> _activePackets = [];
    private readonly Dictionary<string, IPacketHandler> _handlers;
    private readonly IDeviceService _deviceService;
    private readonly IStatisticsService _statistics;
    private IPacketHandler _activeHandler;
    private IPacketHandler? _networkBuildHandler;
    private SimulationConfig _config = new();
    private readonly Random _random = new();

    public PacketService(IDeviceService deviceService, IStatisticsService statistics, IEnumerable<IPacketHandler> handlers)
    {
        _deviceService = deviceService;
        _statistics = statistics;
        _handlers = handlers.Where(h => h.Name != "NetworkBuild").ToDictionary(h => h.Name);
        _networkBuildHandler = handlers.FirstOrDefault(h => h.Name == "NetworkBuild");
        _activeHandler = _handlers.Values.FirstOrDefault() 
            ?? throw new InvalidOperationException("At least one packet handler must be registered");
    }

    public IReadOnlyCollection<Packet> ActivePackets => _activePackets;

    public IReadOnlyDictionary<string, IPacketHandler> Handlers => _handlers;

    public IPacketHandler ActiveHandler => _activeHandler;

    public void SetNetworkBuildHandler(IPacketHandler handler)
    {
        _networkBuildHandler = handler;
    }

    public void SetConfig(SimulationConfig config)
    {
        _config = config;
    }

    public Packet CreatePacket(Device sender, Device? receiver, PacketType type, byte[]? payload = null)
    {
        var packet = new Packet(sender, receiver)
        {
            PacketType = type,
            Payload = payload
        };
        
        // Для NetworkBuild пакетов NextHop = receiver (идут напрямую)
        if (type == PacketType.NetworkBuild && receiver is not null)
        {
            packet.NextHop = receiver;
            packet.IsInitial = false;
        }
        
        RegisterPacket(packet);
        return packet;
    }

    public Packet CreatePing(Device sender, Device receiver)
    {
        return CreatePacket(sender, receiver, PacketType.Ping, "PING"u8.ToArray());
    }

    public Packet CreateLampCommand(Device sender, Lamp lamp, bool turnOn)
    {
        var payload = new byte[] { turnOn ? (byte)1 : (byte)0 };
        return CreatePacket(sender, lamp, PacketType.LampCommand, payload);
    }

    public Packet CreateSensorData(Sensor sensor, Device receiver, double value)
    {
        var payload = BitConverter.GetBytes(value);
        return CreatePacket(sensor, receiver, PacketType.SensorData, payload);
    }

    public Packet CreateNetworkBuild(Device sender, Guid targetDeviceId)
    {
        return CreatePacket(sender, null, PacketType.NetworkBuild, targetDeviceId.ToByteArray());
    }

    public void RegisterPacket(Packet packet)
    {
        _activePackets.Add(packet);
    }

    public void TerminatePacket(Packet packet)
    {
        _activePackets.Remove(packet);
    }

    public List<Vector2> Tick()
    {
        var renderData = new List<Vector2>();
        var packetsCopy = _activePackets.ToList();
        var speed = _config.PacketSpeed;
        
        foreach (var p in packetsCopy)
        {
            if (p.CurrentHop is null || p.NextHop is null)
            {
                continue;
            }
            
            // Если пакет только создан - передаём обработчику для определения маршрута
            if (p.IsInitial)
            {
                ProcessInitialPacket(p);
                continue;
            }
            
            // Для обычных пакетов проверяем что есть связь между CurrentHop и NextHop
            // NetworkBuild пакеты идут напрямую (они устанавливают связи)
            if (p.PacketType != PacketType.NetworkBuild)
            {
                bool hasConnection = p.CurrentHop.Connections.Contains(p.NextHop) || 
                                    p.NextHop.Connections.Contains(p.CurrentHop);
                if (!hasConnection)
                {
                    // Нет связи - пакет теряется (ошибка маршрутизации)
                    float dist = Vector2.Distance(p.CurrentHop.Pos, p.NextHop.Pos);
                    _statistics.RecordPacketLost(p, dist, 1.0);
                    _activePackets.Remove(p);
                    continue;
                }
            }
            
            float distance = Vector2.Distance(p.CurrentHop.Pos, p.NextHop.Pos);
            if (distance < 1e-6f)
            {
                DeliverPacket(p, distance);
                continue;
            }
            
            float timeElapsed = (float)(DateTime.UtcNow - p.CreatedOn).TotalSeconds;
            float t = MathF.Min(timeElapsed * speed / distance, 1f);
            
            if (t >= 1f - 1e-9)
            {
                DeliverPacket(p, distance);
                continue;
            }
            
            // Добавляем позицию для отрисовки
            renderData.Add(Vector2.Lerp(p.CurrentHop.Pos, p.NextHop.Pos, t));
        }

        return renderData;
    }

    private void ProcessInitialPacket(Packet packet)
    {
        packet.IsInitial = false;
        _activePackets.Remove(packet);
        
        // Обычный пакет - обработчик определяет маршрут
        _activeHandler.Handle(packet.CurrentHop!, packet, this, _deviceService, _statistics);
    }

    private void DeliverPacket(Packet packet, float distance)
    {
        // Проверяем packet loss (не для NetworkBuild пакетов)
        if (packet.PacketType != PacketType.NetworkBuild && ShouldLosePacket(distance))
        {
            var lossChance = CalculateLossChance(distance);
            _statistics.RecordPacketLost(packet, distance, lossChance);
            _activePackets.Remove(packet);
            return;
        }
        
        // Записываем статистику хопа
        _statistics.RecordHop(packet, packet.NextHop!.Id);
        
        if (packet.PacketType != PacketType.NetworkBuild)
        {
            _statistics.RecordPacketForwarded(packet, packet.CurrentHop!.Id, packet.NextHop.Id, distance);
        }
        
        // Доставляем пакет на NextHop
        if (packet.PacketType == PacketType.NetworkBuild && _networkBuildHandler is not null)
        {
            _networkBuildHandler.Handle(packet.NextHop!, packet, this, _deviceService, _statistics);
        }
        else
        {
            _activeHandler.Handle(packet.NextHop!, packet, this, _deviceService, _statistics);
        }
        
        _activePackets.Remove(packet);
    }

    private bool ShouldLosePacket(float distance)
    {
        var lossChance = CalculateLossChance(distance);
        return _random.NextDouble() < lossChance;
    }

    private double CalculateLossChance(float distance)
    {
        return _config.BasePacketLossRate + (_config.DistancePacketLossRate * distance / 100.0);
    }

    public void Clear()
    {
        _activePackets.Clear();
    }

    public void SetActiveHandler(string name)
    {
        if (!_handlers.TryGetValue(name, out var handler))
        {
            throw new KeyNotFoundException($"Handler '{name}' not found.");
        }
        _activeHandler = handler;
    }

    public IEnumerable<string> GetHandlerNames() => _handlers.Keys;
}
