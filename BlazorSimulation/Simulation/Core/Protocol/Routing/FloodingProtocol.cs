namespace Core.Protocol.Routing;

/// <summary>
/// Широковещательный протокол маршрутизации (flooding)
/// Изолирован от симуляции - можно использовать в реальном устройстве
/// </summary>
public class FloodingProtocol : IRoutingProtocol
{
    private INetworkNode? _node;
    private readonly HashSet<Guid> _seenMessages = [];
    private readonly object _lock = new();

    public string Name => "Flooding";
    public string Description => "Широковещательный протокол - отправляет пакет всем соседям";

    public event Action<ProtocolMessage>? OnMessageReceived;
    public event Action<ProtocolMessage>? OnMessageDelivered;

    public void Initialize(INetworkNode node)
    {
        _node = node;
    }

    public void HandleMessage(ProtocolMessage message, Guid fromNodeId)
    {
        if (_node is null) return;

        lock (_lock)
        {
            // Проверяем, видели ли мы это сообщение
            if (!_seenMessages.Add(message.MessageId))
            {
                return; // Уже обработано
            }

            // Ограничиваем размер кеша
            if (_seenMessages.Count > 10000)
            {
                _seenMessages.Clear();
                _seenMessages.Add(message.MessageId);
            }
        }

        // Сообщение для нас?
        if (message.ReceiverId == _node.NodeId || message.ReceiverId is null)
        {
            OnMessageReceived?.Invoke(message);
            
            if (message.ReceiverId == _node.NodeId)
            {
                OnMessageDelivered?.Invoke(message);
                return; // Не пересылаем дальше
            }
        }

        // Пересылаем всем соседям кроме отправителя
        if (message.TTL > 0)
        {
            message.TTL--;
            message.HopCount++;
            
            var data = MessageSerializer.Serialize(message);
            foreach (var neighborId in _node.GetNeighbors())
            {
                if (neighborId != fromNodeId && _node.IsNeighborReachable(neighborId))
                {
                    _node.Send(neighborId, data);
                }
            }
        }
    }

    public void SendTo(Guid targetNodeId, ProtocolMessage message)
    {
        if (_node is null) return;

        message.SenderId = _node.NodeId;
        message.ReceiverId = targetNodeId;
        
        Broadcast(message);
    }

    public void Broadcast(ProtocolMessage message)
    {
        if (_node is null) return;

        message.SenderId = _node.NodeId;
        
        lock (_lock)
        {
            _seenMessages.Add(message.MessageId);
        }

        var data = MessageSerializer.Serialize(message);
        foreach (var neighborId in _node.GetNeighbors())
        {
            if (_node.IsNeighborReachable(neighborId))
            {
                _node.Send(neighborId, data);
            }
        }
    }

    public Guid? GetNextHop(Guid targetNodeId)
    {
        // Flooding не имеет таблицы маршрутизации
        return null;
    }
}
