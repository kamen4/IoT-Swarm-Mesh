using Common.Dto;

namespace BusinessServer.Services;

/// <summary>Defines the contract for dispatching pin toggle commands to the gateway device via Redis and resolving the resulting hardware acknowledgements.</summary>
public interface IPinDispatchService
{
    /// <summary>
    /// Publishes a pin toggle command to the hub:cmd Redis channel and waits for the hardware acknowledgement
    /// arriving on hub:evt, correlated by a unique ID embedded in both messages.
    /// Throws <see cref="TimeoutException"/> if no response is received within the allowed window.
    /// </summary>
    /// <param name="pin">GPIO pin number on the gateway device to toggle.</param>
    /// <param name="ct">Cancellation token to abort the wait.</param>
    /// <returns>The confirmed pin number and its new state as reported by the device.</returns>
    Task<PinToggleResponse> TogglePinAsync(int pin, CancellationToken ct = default);

    /// <summary>
    /// Signals the pending toggle operation identified by <paramref name="correlationId"/> that the hardware
    /// has acknowledged the command with the reported pin state.
    /// Called by <see cref="PinEventListener"/> when a hub:evt message arrives.
    /// </summary>
    /// <param name="correlationId">The correlation ID embedded in the original hub:cmd message.</param>
    /// <param name="pin">GPIO pin number reported by the device.</param>
    /// <param name="state">New pin state reported by the device (e.g., 0 = low, 1 = high).</param>
    void ResolveEvent(string correlationId, int pin, int state);
}
