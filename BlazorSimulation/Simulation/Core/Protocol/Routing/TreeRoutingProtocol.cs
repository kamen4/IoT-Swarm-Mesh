namespace Core.Protocol.Routing;

/// <summary>
/// Простой протокол маршрутизации по дереву
/// Каждый узел знает путь к корню и своих потомков
/// </summary>
public class TreeRoutingProtocol : IRoutingProtocol
{
    private INetworkNode? _node;
    private Guid? _parentId;
    private readonly HashSet<Guid> _children = [];
    private readonly Dictionary<Guid, Guid> _routingTable = []; // destination -> nextHop
    private readonly HashSet<Guid> _seenMessages = [];
    private readonly object _lock = new();

    public string Name => "Tree";
    public string Description => "Маршрутизация по дереву - каждый узел знает родителя и детей";

    public Guid? ParentId => _parentId;
    public IReadOnlySet<Guid> Children => _children;

    public event Action<ProtocolMessage>? OnMessageReceived;
    public event Action<ProtocolMessage>? OnMessageDelivered;

    public void Initialize(INetworkNode node)
    {
        _node = node;
    }

    public void SetParent(Guid parentId)
    {
        _parentId = parentId;
    }

    public void AddChild(Guid childId)
    {
        lock (_lock)
        {
            _children.Add(childId);
        }
    }

    public void UpdateRoutingTable(Guid destination, Guid nextHop)
    {
        lock (_lock)
        {
            _routingTable[destination] = nextHop;
        }
    }

    public void HandleMessage(ProtocolMessage message, Guid fromNodeId)
    {
        if (_node is null) return;

        lock (_lock)
        {
            if (!_seenMessages.Add(message.MessageId))
            {
                return;
            }

            if (_seenMessages.Count > 10000)
            {
                _seenMessages.Clear();
                _seenMessages.Add(message.MessageId);
            }
        }

        // Сообщение для нас
        if (message.ReceiverId == _node.NodeId)
        {
            OnMessageReceived?.Invoke(message);
            OnMessageDelivered?.Invoke(message);
            return;
        }

        // Широковещательное
        if (message.ReceiverId is null)
        {
            OnMessageReceived?.Invoke(message);
            ForwardBroadcast(message, fromNodeId);
            return;
        }

        // Маршрутизация к цели
        if (message.TTL > 0)
        {
            var nextHop = GetNextHop(message.ReceiverId.Value);
            if (nextHop.HasValue && _node.IsNeighborReachable(nextHop.Value))
            {
                message.TTL--;
                message.HopCount++;
                _node.Send(nextHop.Value, MessageSerializer.Serialize(message));
            }
        }
    }

    private void ForwardBroadcast(ProtocolMessage message, Guid fromNodeId)
    {
        if (_node is null || message.TTL <= 0) return;

        message.TTL--;
        message.HopCount++;
        var data = MessageSerializer.Serialize(message);

        // Если пришло от родителя - отправляем детям
        // Если пришло от ребенка - отправляем родителю и другим детям
        lock (_lock)
        {
            if (fromNodeId == _parentId)
            {
                foreach (var childId in _children)
                {
                    if (_node.IsNeighborReachable(childId))
                    {
                        _node.Send(childId, data);
                    }
                }
            }
            else
            {
                if (_parentId.HasValue && _node.IsNeighborReachable(_parentId.Value))
                {
                    _node.Send(_parentId.Value, data);
                }
                foreach (var childId in _children.Where(c => c != fromNodeId))
                {
                    if (_node.IsNeighborReachable(childId))
                    {
                        _node.Send(childId, data);
                    }
                }
            }
        }
    }

    public void SendTo(Guid targetNodeId, ProtocolMessage message)
    {
        if (_node is null) return;

        message.SenderId = _node.NodeId;
        message.ReceiverId = targetNodeId;

        var nextHop = GetNextHop(targetNodeId);
        if (nextHop.HasValue && _node.IsNeighborReachable(nextHop.Value))
        {
            lock (_lock)
            {
                _seenMessages.Add(message.MessageId);
            }
            _node.Send(nextHop.Value, MessageSerializer.Serialize(message));
        }
    }

    public void Broadcast(ProtocolMessage message)
    {
        if (_node is null) return;

        message.SenderId = _node.NodeId;
        message.ReceiverId = null;

        lock (_lock)
        {
            _seenMessages.Add(message.MessageId);
        }

        var data = MessageSerializer.Serialize(message);

        // Отправляем родителю и всем детям
        if (_parentId.HasValue && _node.IsNeighborReachable(_parentId.Value))
        {
            _node.Send(_parentId.Value, data);
        }

        foreach (var childId in _children)
        {
            if (_node.IsNeighborReachable(childId))
            {
                _node.Send(childId, data);
            }
        }
    }

    public Guid? GetNextHop(Guid targetNodeId)
    {
        lock (_lock)
        {
            // Проверяем таблицу маршрутизации
            if (_routingTable.TryGetValue(targetNodeId, out var nextHop))
            {
                return nextHop;
            }

            // Если цель - наш ребенок
            if (_children.Contains(targetNodeId))
            {
                return targetNodeId;
            }

            // Иначе отправляем родителю
            return _parentId;
        }
    }
}
