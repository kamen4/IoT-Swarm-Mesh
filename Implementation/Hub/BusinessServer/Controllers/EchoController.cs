using Common.Dto;
using Microsoft.AspNetCore.Mvc;

namespace BusinessServer.Controllers;

/// <summary>Diagnostic controller that reflects messages back to the caller, useful for connectivity and latency checks.</summary>
[ApiController]
[Route("api/[controller]")]
public class EchoController : ControllerBase
{
    /// <summary>
    /// Accepts a message in the request body and returns it verbatim together with the server-side receipt timestamp.
    /// </summary>
    /// <param name="request">Request body containing the message to echo.</param>
    /// <returns>An <see cref="EchoResponse"/> with the echoed message and the UTC time the request was processed.</returns>
    [HttpPost]
    public ActionResult<EchoResponse> Echo([FromBody] EchoRequest request)
    {
        return Ok(new EchoResponse(request.Message, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Accepts an optional message as a query parameter and returns it together with the server-side receipt timestamp.
    /// Defaults to "ping" when no message is supplied.
    /// </summary>
    /// <param name="message">The text to echo; defaults to "ping".</param>
    /// <returns>An <see cref="EchoResponse"/> with the echoed message and the UTC time the request was processed.</returns>
    [HttpGet]
    public ActionResult<EchoResponse> EchoGet([FromQuery] string message = "ping")
    {
        return Ok(new EchoResponse(message, DateTimeOffset.UtcNow));
    }
}
