using Core.Devices;
using Core.Statistics;

namespace Core.Services;

public class SimulationService : ISimulationService
{
    private readonly IDeviceService _devices;
    private readonly IPacketService _packets;
    private readonly INetworkBuilderService _networkBuilder;
    private readonly IStatisticsService _statistics;
    private readonly ISerializationService _serialization;
    
    private CancellationTokenSource? _simulationCts;
    private SimulationState _state = SimulationState.Idle;
    private SimulationConfig _currentConfig = new();
    private readonly Random _random = new();
    private DateTime _lastTickTime = DateTime.UtcNow;
    private DateTime _lastPacketTime = DateTime.UtcNow;

    public SimulationService(
        IDeviceService devices,
        IPacketService packets,
        INetworkBuilderService networkBuilder,
        IStatisticsService statistics,
        ISerializationService serialization)
    {
        _devices = devices;
        _packets = packets;
        _networkBuilder = networkBuilder;
        _statistics = statistics;
        _serialization = serialization;

        _networkBuilder.OnProgress += OnNetworkBuildProgress;
    }

    public IDeviceService Devices => _devices;
    public IPacketService Packets => _packets;
    public INetworkBuilderService NetworkBuilder => _networkBuilder;
    public IStatisticsService Statistics => _statistics;
    public ISerializationService Serialization => _serialization;

    public SimulationState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                OnStateChanged?.Invoke(_state);
            }
        }
    }

    public event Action<SimulationState>? OnStateChanged;
    public event Action<PacketEventArgs>? OnPacketEvent;

    public async Task StartNetworkBuildAsync()
    {
        var hub = _devices.Hub;
        if (hub is null)
        {
            throw new InvalidOperationException("No hub found in devices");
        }

        State = SimulationState.BuildingNetwork;
        _statistics.Reset();

        try
        {
            await _networkBuilder.BuildNetworkAsync(hub);
        }
        finally
        {
            State = SimulationState.Idle;
        }
    }

    public async Task StartDataSimulationAsync(SimulationConfig config)
    {
        if (State == SimulationState.Running) return;

        _currentConfig = config;
        _packets.SetConfig(config);
        _simulationCts = new CancellationTokenSource();
        State = SimulationState.Running;
        _statistics.Reset();
        _lastTickTime = DateTime.UtcNow;
        _lastPacketTime = DateTime.UtcNow;

        try
        {
            var endTime = DateTime.UtcNow.AddMilliseconds(config.SimulationDurationMs);
            
            while (DateTime.UtcNow < endTime && !_simulationCts.Token.IsCancellationRequested)
            {
                await Task.Delay(16, _simulationCts.Token);
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when stopped
        }
        finally
        {
            State = SimulationState.Completed;
            _statistics.Current.EndTime = DateTime.UtcNow;
            _statistics.Current.SimulationDuration = DateTime.UtcNow - _statistics.Current.StartTime;
        }
    }

    public void StopSimulation()
    {
        _simulationCts?.Cancel();
        _simulationCts = null;
        if (State != SimulationState.Idle)
        {
            State = SimulationState.Idle;
        }
    }

    public void Reset()
    {
        StopSimulation();
        _packets.Clear();
        _networkBuilder.ClearConnections();
        _statistics.Reset();
        State = SimulationState.Idle;
    }

    public SimulationTickResult Tick()
    {
        var now = DateTime.UtcNow;
        var deltaTime = (now - _lastTickTime).TotalSeconds;
        _lastTickTime = now;

        // Drain batteries
        if (State == SimulationState.Running)
        {
            foreach (var device in _devices.All)
            {
                if (device.DevicePowerType == Device.PowerType.Battery)
                {
                    var drain = _currentConfig.BatteryDrainPerSecond * deltaTime * device.BatteryDrainRate;
                    device.Battery = Math.Max(0, device.Battery - drain);
                    _statistics.RecordBatteryDrain(device.Id, drain);
                }
            }

            // Generate random packets
            GenerateRandomPackets(now);
        }

        var positions = _packets.Tick();

        return new SimulationTickResult
        {
            PacketPositions = positions,
            HasActivePackets = _packets.ActivePackets.Count > 0
        };
    }

    private void GenerateRandomPackets(DateTime now)
    {
        var timeSinceLastPacket = (now - _lastPacketTime).TotalMilliseconds;
        if (timeSinceLastPacket < _currentConfig.PacketIntervalMs) return;

        _lastPacketTime = now;
        var hub = _devices.Hub;
        if (hub is null) return;

        // Random lamp commands from hub
        if (_currentConfig.RandomLampCommands)
        {
            var lamps = _devices.Lamps.ToList();
            if (lamps.Count > 0)
            {
                var lamp = lamps[_random.Next(lamps.Count)];
                var turnOn = _random.Next(2) == 1;
                var packet = _packets.CreateLampCommand(hub, lamp, turnOn);
                _statistics.RecordPacketCreated(packet);
                RaisePacketEvent(packet, PacketEventType.Created);
            }
        }

        // Random sensor data to hub
        if (_currentConfig.RandomSensorData)
        {
            var sensors = _devices.Sensors.ToList();
            if (sensors.Count > 0)
            {
                var sensor = sensors[_random.Next(sensors.Count)];
                var value = sensor.GenData();
                var packet = _packets.CreateSensorData(sensor, hub, value);
                _statistics.RecordPacketCreated(packet);
                RaisePacketEvent(packet, PacketEventType.Created);
            }
        }
    }

    private void OnNetworkBuildProgress(NetworkBuildProgress progress)
    {
        // Progress handling - state change happens in StartNetworkBuildAsync
    }

    private void RaisePacketEvent(Packet packet, PacketEventType eventType)
    {
        OnPacketEvent?.Invoke(new PacketEventArgs
        {
            Packet = packet,
            EventType = eventType
        });
    }
}
