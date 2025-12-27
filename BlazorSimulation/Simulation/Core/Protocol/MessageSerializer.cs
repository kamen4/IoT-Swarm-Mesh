namespace Core.Protocol;

/// <summary>
/// Сериализатор сообщений протокола
/// Формат: [1 byte Type][16 bytes MessageId][16 bytes SenderId][1 byte HasReceiver][16 bytes ReceiverId?][4 bytes TTL][4 bytes HopCount][N bytes Payload]
/// </summary>
public static class MessageSerializer
{
    public static byte[] Serialize(ProtocolMessage message)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        writer.Write((byte)message.Type);
        writer.Write(message.MessageId.ToByteArray());
        writer.Write(message.SenderId.ToByteArray());
        writer.Write(message.ReceiverId.HasValue);
        if (message.ReceiverId.HasValue)
        {
            writer.Write(message.ReceiverId.Value.ToByteArray());
        }
        writer.Write(message.TTL);
        writer.Write(message.HopCount);
        writer.Write(message.Timestamp.ToBinary());
        
        // Payload serialization
        var payload = message.Serialize();
        writer.Write(payload.Length);
        writer.Write(payload);
        
        return ms.ToArray();
    }

    public static ProtocolMessage? Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        
        var type = (MessageType)reader.ReadByte();
        var messageId = new Guid(reader.ReadBytes(16));
        var senderId = new Guid(reader.ReadBytes(16));
        var hasReceiver = reader.ReadBoolean();
        Guid? receiverId = hasReceiver ? new Guid(reader.ReadBytes(16)) : null;
        var ttl = reader.ReadInt32();
        var hopCount = reader.ReadInt32();
        var timestamp = DateTime.FromBinary(reader.ReadInt64());
        
        var payloadLength = reader.ReadInt32();
        var payload = reader.ReadBytes(payloadLength);
        
        ProtocolMessage? message = type switch
        {
            MessageType.Ping => PingMessage.FromPayload(payload),
            MessageType.Ack => AckMessage.FromPayload(payload),
            MessageType.LampCommand => LampCommandMessage.FromPayload(payload),
            MessageType.SensorData => SensorDataMessage.FromPayload(payload),
            MessageType.NetworkBuild => NetworkBuildMessage.FromPayload(payload),
            MessageType.Data => DataMessage.FromPayload(payload),
            _ => null
        };
        
        if (message is not null)
        {
            message.MessageId = messageId;
            message.SenderId = senderId;
            message.ReceiverId = receiverId;
            message.TTL = ttl;
            message.HopCount = hopCount;
            message.Timestamp = timestamp;
        }
        
        return message;
    }
}
