using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using BusinessServer.Services;

namespace BusinessServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceRegistryService _registry;

    public DevicesController(IDeviceRegistryService registry)
    {
        _registry = registry;
    }

    [HttpGet]
    public ActionResult<IEnumerable<DeviceInfo>> GetAll()
    {
        return Ok(_registry.GetAll());
    }

    [HttpGet("{deviceId}")]
    public ActionResult<DeviceInfo> GetById(string deviceId)
    {
        var device = _registry.GetById(deviceId);
        if (device is null) return NotFound();
        return Ok(device);
    }

    [HttpPost]
    public ActionResult<DeviceInfo> Register([FromBody] DeviceInfo device)
    {
        var registered = _registry.Register(device);
        return CreatedAtAction(nameof(GetById), new { deviceId = registered.DeviceId }, registered);
    }
}
