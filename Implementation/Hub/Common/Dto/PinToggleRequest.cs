namespace Common.Dto;

/// <summary>HTTP request: toggle a GPIO pin on the gateway device.</summary>
public record PinToggleRequest(int Pin);
