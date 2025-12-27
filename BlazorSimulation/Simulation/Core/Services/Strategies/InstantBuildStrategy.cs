using Core.Devices;

namespace Core.Services.Strategies;

/// <summary>
/// Мгновенное построение сети через пакеты - отправляет все пакеты сразу
/// </summary>
public class InstantBuildStrategy : INetworkBuildStrategy
{
    public string Name => "Мгновенное";
    public string Description => "Мгновенно отправляет Discovery пакеты всем устройствам";

    public async Task BuildAsync(Hub hub, IDeviceService deviceService, IPacketService packetService,
        Action<NetworkBuildProgress> onProgress, CancellationToken cancellationToken)
    {
        var allDevices = deviceService.All.ToList();
        
        // Отправляем Discovery пакеты всем видимым парам
        var sentPairs = new HashSet<(Guid, Guid)>();
        
        foreach (var (d1, d2) in deviceService.GetAllVisibilities())
        {
            // Отправляем пакет только в одном направлении
            var key = d1.Id.CompareTo(d2.Id) < 0 ? (d1.Id, d2.Id) : (d2.Id, d1.Id);
            if (sentPairs.Add(key))
            {
                var packet = packetService.CreatePacket(d1, d2, PacketType.NetworkBuild);
                packet.Payload = BuildPayload(d1.Id);
            }
        }

        // Ждём пока все пакеты дойдут
        int iteration = 0;
        int maxIterations = 50;
        
        while (iteration++ < maxIterations && !cancellationToken.IsCancellationRequested)
        {
            packetService.Tick();
            
            if (packetService.ActivePackets.Count == 0)
            {
                break;
            }
            
            await Task.Delay(20, cancellationToken);
        }

        var connectedCount = CountConnectedDevices(hub, allDevices);

        onProgress(new NetworkBuildProgress
        {
            ConnectedDevices = connectedCount,
            TotalDevices = allDevices.Count,
            RetryCount = 0,
            IsComplete = true,
            Message = "Сеть построена"
        });
    }

    public void AcceptBuildPacket(Device device, Packet packet, IDeviceService deviceService)
    {
        if (packet.Payload is null || packet.Payload.Length < 16) return;
        
        var sender = packet.Sender;
        
        // Проверяем что это не тот же самый девайс
        if (sender.Id == device.Id) return;
        
        // Устанавливаем двустороннюю связь
        if (!device.Connections.Contains(sender))
        {
            device.Connections.Add(sender);
        }
        if (!sender.Connections.Contains(device))
        {
            sender.Connections.Add(device);
        }
    }

    private static byte[] BuildPayload(Guid deviceId)
    {
        return deviceId.ToByteArray();
    }

    private static int CountConnectedDevices(Hub hub, List<Device> allDevices)
    {
        var connected = new HashSet<Device> { hub };
        var stack = new Stack<Device>();
        stack.Push(hub);
        
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var neighbor in current.Connections)
            {
                if (connected.Add(neighbor))
                {
                    stack.Push(neighbor);
                }
            }
        }
        
        return connected.Count;
    }
}
