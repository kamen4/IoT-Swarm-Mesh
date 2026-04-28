namespace Common.Dto;

/// <summary>Request payload for the echo endpoint, containing a message to be reflected back by the server.</summary>
public record EchoRequest(string Message);
