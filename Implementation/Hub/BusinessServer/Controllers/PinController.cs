using BusinessServer.Services;
using Common.Dto;
using Microsoft.AspNetCore.Mvc;

namespace BusinessServer.Controllers;

/// <summary>HTTP API controller for sending GPIO pin control commands to the gateway device via Redis.</summary>
[ApiController, Route("api/pin")]
public class PinController : ControllerBase
{
    private readonly IPinDispatchService _pinDispatch;

    /// <summary>Initializes a new instance of <see cref="PinController"/> with the required dispatch service.</summary>
    /// <param name="pinDispatch">Service that publishes pin commands and awaits hardware confirmation.</param>
    public PinController(IPinDispatchService pinDispatch)
    {
        _pinDispatch = pinDispatch;
    }

    /// <summary>
    /// Sends a pin toggle command to the gateway device and waits for the hardware acknowledgement.
    /// Returns 200 with the confirmed pin state on success, or 504 if the device does not respond within the timeout.
    /// </summary>
    /// <param name="request">The pin number to toggle.</param>
    /// <param name="ct">Cancellation token for the HTTP request lifetime.</param>
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] PinToggleRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _pinDispatch.TogglePinAsync(request.Pin, ct);
            return Ok(result);
        }
        catch (TimeoutException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                new { Error = "Gateway device did not respond in time." });
        }
    }
}
