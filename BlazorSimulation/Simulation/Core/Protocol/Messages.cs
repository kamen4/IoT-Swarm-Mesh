namespace Core.Protocol;

public class PingMessage : ProtocolMessage
{
    public override MessageType Type => MessageType.Ping;
    
    public override byte[] Serialize() => [];
    
    public static PingMessage FromPayload(byte[] payload) => new();
}

public class AckMessage : ProtocolMessage
{
    public Guid OriginalMessageId { get; set; }
    
    public override MessageType Type => MessageType.Ack;
    
    public override byte[] Serialize() => OriginalMessageId.ToByteArray();
    
    public static AckMessage FromPayload(byte[] payload) => new()
    {
        OriginalMessageId = new Guid(payload)
    };
}

public class DataMessage : ProtocolMessage
{
    public byte[] Data { get; set; } = [];
    
    public override MessageType Type => MessageType.Data;
    
    public override byte[] Serialize() => Data;
    
    public static DataMessage FromPayload(byte[] payload) => new()
    {
        Data = payload
    };
}

public class LampCommandMessage : ProtocolMessage
{
    public bool TurnOn { get; set; }
    
    public override MessageType Type => MessageType.LampCommand;
    
    public override byte[] Serialize() => [TurnOn ? (byte)1 : (byte)0];
    
    public static LampCommandMessage FromPayload(byte[] payload) => new()
    {
        TurnOn = payload.Length > 0 && payload[0] == 1
    };
}

public class SensorDataMessage : ProtocolMessage
{
    public double Value { get; set; }
    
    public override MessageType Type => MessageType.SensorData;
    
    public override byte[] Serialize() => BitConverter.GetBytes(Value);
    
    public static SensorDataMessage FromPayload(byte[] payload) => new()
    {
        Value = payload.Length >= 8 ? BitConverter.ToDouble(payload) : 0
    };
}

public class NetworkBuildMessage : ProtocolMessage
{
    public Guid TargetDeviceId { get; set; }
    
    public override MessageType Type => MessageType.NetworkBuild;
    
    public override byte[] Serialize() => TargetDeviceId.ToByteArray();
    
    public static NetworkBuildMessage FromPayload(byte[] payload) => new()
    {
        TargetDeviceId = payload.Length >= 16 ? new Guid(payload) : Guid.Empty
    };
}
