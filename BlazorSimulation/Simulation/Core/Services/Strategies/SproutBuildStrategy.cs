using Core.Devices;
using System.Numerics;

namespace Core.Services.Strategies;

/// <summary>
/// Стратегия построения сети через пакеты - имитирует реальный протокол обнаружения
/// Hub отправляет Discovery пакеты, устройства отвечают и сеть разрастается волнами
/// </summary>
public class SproutBuildStrategy : INetworkBuildStrategy
{
    private readonly HashSet<Guid> _connectedDevices = [];
    private int _wave = 0;

    public string Name => "Sprout";
    public string Description => "Построение сети через Discovery пакеты (волнами от хаба)";

    public async Task BuildAsync(Hub hub, IDeviceService deviceService, IPacketService packetService,
        Action<NetworkBuildProgress> onProgress, CancellationToken cancellationToken)
    {
        _connectedDevices.Clear();
        _wave = 0;
        
        var allDevices = deviceService.All.ToList();
        _connectedDevices.Add(hub.Id);

        int maxWaves = 20;
        int prevConnectedCount = 0;
        
        while (_wave < maxWaves && !cancellationToken.IsCancellationRequested)
        {
            _wave++;
            
            // Получаем текущее подключённое множество
            var connected = GetConnectedDevices(hub);
            
            // Отправляем Discovery от всех подключённых к их видимым неподключённым соседям
            int packetsSent = 0;
            foreach (var device in connected)
            {
                foreach (var visible in deviceService.GetVisibleDevices(device))
                {
                    if (!_connectedDevices.Contains(visible.Id))
                    {
                        // Отправляем Discovery - при построении сети пакеты идут напрямую
                        // (видимость = возможность связи)
                        var packet = packetService.CreatePacket(device, visible, PacketType.NetworkBuild);
                        packet.Payload = BuildPayload(device.Id);
                        packetsSent++;
                    }
                }
            }
            
            if (packetsSent == 0)
            {
                // Нет новых устройств для подключения
                break;
            }
            
            // Ждём и обрабатываем пакеты
            int tickCount = 0;
            while (packetService.ActivePackets.Count > 0 && tickCount++ < 50)
            {
                await Task.Delay(30, cancellationToken);
                packetService.Tick();
            }
            
            var connectedCount = _connectedDevices.Count;
            
            onProgress(new NetworkBuildProgress
            {
                ConnectedDevices = connectedCount,
                TotalDevices = allDevices.Count,
                RetryCount = _wave,
                IsComplete = connectedCount >= allDevices.Count,
                Message = $"Волна {_wave}: подключено {connectedCount}/{allDevices.Count}"
            });
            
            // Если ничего не подключилось - выходим
            if (connectedCount == prevConnectedCount)
            {
                break;
            }
            prevConnectedCount = connectedCount;
            
            if (connectedCount >= allDevices.Count)
            {
                break;
            }
        }

        onProgress(new NetworkBuildProgress
        {
            ConnectedDevices = _connectedDevices.Count,
            TotalDevices = allDevices.Count,
            RetryCount = _wave,
            IsComplete = true,
            Message = _connectedDevices.Count >= allDevices.Count 
                ? "Сеть построена" 
                : $"Построение завершено ({_connectedDevices.Count}/{allDevices.Count} подключено)"
        });
    }

    private HashSet<Device> GetConnectedDevices(Hub hub)
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
        
        return connected;
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
        
        _connectedDevices.Add(device.Id);
        _connectedDevices.Add(sender.Id);
    }

    private static byte[] BuildPayload(Guid deviceId)
    {
        return deviceId.ToByteArray();
    }
}
