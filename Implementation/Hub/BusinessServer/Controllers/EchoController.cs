using Common.Dto;
using Microsoft.AspNetCore.Mvc;

namespace BusinessServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EchoController : ControllerBase
{
    [HttpPost]
    public ActionResult<EchoResponse> Echo([FromBody] EchoRequest request)
    {
        return Ok(new EchoResponse(request.Message, DateTimeOffset.UtcNow));
    }

    [HttpGet]
    public ActionResult<EchoResponse> EchoGet([FromQuery] string message = "ping")
    {
        return Ok(new EchoResponse(message, DateTimeOffset.UtcNow));
    }
}
