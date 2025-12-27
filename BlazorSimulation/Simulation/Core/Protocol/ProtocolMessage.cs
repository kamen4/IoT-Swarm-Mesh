namespace Core.Protocol;

/// <summary>
/// Базовое сообщение протокола - изолировано от симуляции
/// Можно использовать для реального протокола
/// </summary>
public abstract class ProtocolMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TTL { get; set; } = 64;
    public int HopCount { get; set; } = 0;
    
    public abstract MessageType Type { get; }
    public abstract byte[] Serialize();
    public static ProtocolMessage? Deserialize(byte[] data) => MessageSerializer.Deserialize(data);
}

public enum MessageType : byte
{
    Ping = 0x01,
    Ack = 0x02,
    Data = 0x03,
    NetworkBuild = 0x10,
    LampCommand = 0x20,
    SensorData = 0x21,
    RoutingUpdate = 0x30
}
