using Common.Dto;

namespace BusinessServer.Services;

public interface IPinDispatchService
{
    Task<PinToggleResponse> TogglePinAsync(int pin, CancellationToken ct = default);
    void ResolveEvent(string correlationId, int pin, int state);
}
