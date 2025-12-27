using Core.Devices;

namespace Core.Services;

/// <summary>
/// Сервис построения сети
/// </summary>
public interface INetworkBuilderService
{
    IReadOnlyDictionary<string, INetworkBuildStrategy> Strategies { get; }
    INetworkBuildStrategy ActiveStrategy { get; }
    
    void SetActiveStrategy(string name);
    IEnumerable<string> GetStrategyNames();
    
    Task BuildNetworkAsync(Hub hub, CancellationToken cancellationToken = default);
    void ClearConnections();
    
    bool IsBuilding { get; }
    event Action<NetworkBuildProgress>? OnProgress;
}

/// <summary>
/// Стратегия построения сети
/// </summary>
public interface INetworkBuildStrategy
{
    string Name { get; }
    string Description { get; }
    
    Task BuildAsync(Hub hub, IDeviceService deviceService, IPacketService packetService, 
        Action<NetworkBuildProgress> onProgress, CancellationToken cancellationToken);
    
    void AcceptBuildPacket(Device device, Packet packet, IDeviceService deviceService);
}

public class NetworkBuildProgress
{
    public int ConnectedDevices { get; init; }
    public int TotalDevices { get; init; }
    public int RetryCount { get; init; }
    public bool IsComplete { get; init; }
    public string Message { get; init; } = "";
}
