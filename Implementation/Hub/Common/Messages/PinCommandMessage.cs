namespace Common.Messages;

/// <summary>Redis message sent on channel hub:cmd — commands to the gateway device.</summary>
public record PinCommandMessage(string CorrelationId, int Pin);
