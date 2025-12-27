namespace Core.Protocol;

/// <summary>
/// Интерфейс сетевого узла - изолирован от симуляции
/// Можно реализовать для реального устройства
/// </summary>
public interface INetworkNode
{
    Guid NodeId { get; }
    
    /// <summary>
    /// Отправить сообщение соседу
    /// </summary>
    void Send(Guid neighborId, byte[] data);
    
    /// <summary>
    /// Получить сообщение (вызывается при приходе данных)
    /// </summary>
    void OnReceive(Guid fromNodeId, byte[] data);
    
    /// <summary>
    /// Получить список соседних узлов
    /// </summary>
    IEnumerable<Guid> GetNeighbors();
    
    /// <summary>
    /// Проверить доступность соседа
    /// </summary>
    bool IsNeighborReachable(Guid neighborId);
}

/// <summary>
/// Протокол маршрутизации - изолирован от симуляции
/// </summary>
public interface IRoutingProtocol
{
    string Name { get; }
    string Description { get; }
    
    /// <summary>
    /// Инициализация протокола для узла
    /// </summary>
    void Initialize(INetworkNode node);
    
    /// <summary>
    /// Обработать входящее сообщение
    /// </summary>
    void HandleMessage(ProtocolMessage message, Guid fromNodeId);
    
    /// <summary>
    /// Отправить сообщение на целевой узел
    /// </summary>
    void SendTo(Guid targetNodeId, ProtocolMessage message);
    
    /// <summary>
    /// Широковещательная отправка
    /// </summary>
    void Broadcast(ProtocolMessage message);
    
    /// <summary>
    /// Получить следующий хоп для целевого узла (для маршрутизации)
    /// </summary>
    Guid? GetNextHop(Guid targetNodeId);
}

/// <summary>
/// Протокол построения сети - изолирован от симуляции
/// </summary>
public interface INetworkBuildProtocol
{
    string Name { get; }
    string Description { get; }
    
    /// <summary>
    /// Инициализация протокола для узла
    /// </summary>
    void Initialize(INetworkNode node);
    
    /// <summary>
    /// Запустить построение сети (для корневого узла)
    /// </summary>
    Task BuildNetworkAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Обработать входящее сообщение построения
    /// </summary>
    void HandleBuildMessage(ProtocolMessage message, Guid fromNodeId);
    
    /// <summary>
    /// Событие установки соединения
    /// </summary>
    event Action<Guid, Guid>? OnConnectionEstablished;
}
