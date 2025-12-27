using Core.Protocol;

namespace Tests;

public class ProtocolMessageTests
{
    [Fact]
    public void PingMessage_SerializeDeserialize()
    {
        var original = new PingMessage
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            TTL = 32
        };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized) as PingMessage;

        Assert.NotNull(deserialized);
        Assert.Equal(original.MessageId, deserialized.MessageId);
        Assert.Equal(original.SenderId, deserialized.SenderId);
        Assert.Equal(original.ReceiverId, deserialized.ReceiverId);
        Assert.Equal(original.TTL, deserialized.TTL);
    }

    [Fact]
    public void LampCommandMessage_SerializeDeserialize()
    {
        var original = new LampCommandMessage
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            TurnOn = true
        };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized) as LampCommandMessage;

        Assert.NotNull(deserialized);
        Assert.True(deserialized.TurnOn);
    }

    [Fact]
    public void LampCommandMessage_TurnOff_SerializeDeserialize()
    {
        var original = new LampCommandMessage { TurnOn = false };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized) as LampCommandMessage;

        Assert.NotNull(deserialized);
        Assert.False(deserialized.TurnOn);
    }

    [Fact]
    public void SensorDataMessage_SerializeDeserialize()
    {
        var original = new SensorDataMessage
        {
            SenderId = Guid.NewGuid(),
            Value = 42.5
        };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized) as SensorDataMessage;

        Assert.NotNull(deserialized);
        Assert.Equal(42.5, deserialized.Value);
    }

    [Fact]
    public void NetworkBuildMessage_SerializeDeserialize()
    {
        var targetId = Guid.NewGuid();
        var original = new NetworkBuildMessage
        {
            TargetDeviceId = targetId
        };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized) as NetworkBuildMessage;

        Assert.NotNull(deserialized);
        Assert.Equal(targetId, deserialized.TargetDeviceId);
    }

    [Fact]
    public void AckMessage_SerializeDeserialize()
    {
        var originalMessageId = Guid.NewGuid();
        var original = new AckMessage
        {
            OriginalMessageId = originalMessageId
        };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized) as AckMessage;

        Assert.NotNull(deserialized);
        Assert.Equal(originalMessageId, deserialized.OriginalMessageId);
    }

    [Fact]
    public void DataMessage_SerializeDeserialize()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var original = new DataMessage { Data = data };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized) as DataMessage;

        Assert.NotNull(deserialized);
        Assert.Equal(data, deserialized.Data);
    }

    [Fact]
    public void Message_WithoutReceiver_SerializeDeserialize()
    {
        var original = new PingMessage
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = null
        };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized.ReceiverId);
    }

    [Fact]
    public void Message_TTL_PreservedAfterSerialization()
    {
        var original = new PingMessage { TTL = 10, HopCount = 5 };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized);

        Assert.NotNull(deserialized);
        Assert.Equal(10, deserialized.TTL);
        Assert.Equal(5, deserialized.HopCount);
    }

    [Fact]
    public void Message_Timestamp_PreservedAfterSerialization()
    {
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var original = new PingMessage { Timestamp = timestamp };

        var serialized = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(serialized);

        Assert.NotNull(deserialized);
        Assert.Equal(timestamp, deserialized.Timestamp);
    }
}
