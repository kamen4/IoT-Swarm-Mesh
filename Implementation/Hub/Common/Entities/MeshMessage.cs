namespace Common.Entities;

public class MeshMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string SourceDeviceId { get; set; } = string.Empty;
    public string DestinationDeviceId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
