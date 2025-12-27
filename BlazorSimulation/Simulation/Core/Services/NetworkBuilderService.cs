using Core.Devices;

namespace Core.Services;

public class NetworkBuilderService : INetworkBuilderService
{
    private readonly IDeviceService _deviceService;
    private readonly IPacketService _packetService;
    private readonly Dictionary<string, INetworkBuildStrategy> _strategies;
    private INetworkBuildStrategy _activeStrategy;
    private CancellationTokenSource? _buildCts;

    public NetworkBuilderService(IDeviceService deviceService, IPacketService packetService, 
        IEnumerable<INetworkBuildStrategy> strategies)
    {
        _deviceService = deviceService;
        _packetService = packetService;
        _strategies = strategies.ToDictionary(s => s.Name);
        _activeStrategy = _strategies.Values.FirstOrDefault()
            ?? throw new InvalidOperationException("At least one network build strategy must be registered");
    }

    public IReadOnlyDictionary<string, INetworkBuildStrategy> Strategies => _strategies;

    public INetworkBuildStrategy ActiveStrategy => _activeStrategy;

    public bool IsBuilding { get; private set; }

    public event Action<NetworkBuildProgress>? OnProgress;

    public void SetActiveStrategy(string name)
    {
        if (!_strategies.TryGetValue(name, out var strategy))
        {
            throw new KeyNotFoundException($"Strategy '{name}' not found.");
        }
        _activeStrategy = strategy;
    }

    public IEnumerable<string> GetStrategyNames() => _strategies.Keys;

    public async Task BuildNetworkAsync(Hub hub, CancellationToken cancellationToken = default)
    {
        if (IsBuilding) return;

        IsBuilding = true;
        _buildCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await _activeStrategy.BuildAsync(hub, _deviceService, _packetService, 
                progress => OnProgress?.Invoke(progress), 
                _buildCts.Token);
        }
        finally
        {
            IsBuilding = false;
            _buildCts = null;
        }
    }

    public void ClearConnections()
    {
        _buildCts?.Cancel();
        
        foreach (var device in _deviceService.All)
        {
            device.Connections.Clear();
        }
    }
}
