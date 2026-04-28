namespace Common.Messages;

/// <summary>Redis message sent on channel hub:evt -- events (echo) from the gateway device.</summary>
public record PinEventMessage(string CorrelationId, int Pin, int State);
