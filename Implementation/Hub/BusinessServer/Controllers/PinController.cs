using BusinessServer.Services;
using Common.Dto;
using Microsoft.AspNetCore.Mvc;

namespace BusinessServer.Controllers;

[ApiController, Route("api/pin")]
public class PinController : ControllerBase
{
    private readonly IPinDispatchService _pinDispatch;

    public PinController(IPinDispatchService pinDispatch)
    {
        _pinDispatch = pinDispatch;
    }

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
