namespace Common.Dto;

/// <summary>Response payload returned by the echo endpoint, containing the reflected message and the server-side receipt timestamp.</summary>
public record EchoResponse(string Echo, DateTimeOffset ReceivedAt);
