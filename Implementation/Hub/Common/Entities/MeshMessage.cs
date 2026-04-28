namespace Common.Entities;

/// <summary>Represents a message routed through the IoT swarm mesh network between devices.</summary>
public class MeshMessage
{
    /// <summary>Unique identifier for this message instance, generated automatically on construction.</summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Identifier of the device that originated the message.</summary>
    public string SourceDeviceId { get; set; } = string.Empty;

    /// <summary>Identifier of the device that should receive and process the message.</summary>
    public string DestinationDeviceId { get; set; } = string.Empty;

    /// <summary>Encoded content of the message; interpretation depends on the device type and command.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>UTC timestamp recording when the message was created.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
