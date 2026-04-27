namespace Common.Dto;

/// <summary>HTTP response: confirmed GPIO state after toggle.</summary>
public record PinToggleResponse(int Pin, int State);
