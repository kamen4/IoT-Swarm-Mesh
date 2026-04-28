using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using BusinessServer.Services;

namespace BusinessServer.Controllers;

/// <summary>HTTP API controller for managing the in-memory registry of IoT devices connected to the hub.</summary>
[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceRegistryService _registry;

    /// <summary>Initializes a new instance of <see cref="DevicesController"/> with the required device registry.</summary>
    /// <param name="registry">Service that stores and retrieves registered device entries.</param>
    public DevicesController(IDeviceRegistryService registry)
    {
        _registry = registry;
    }

    /// <summary>Returns the full list of all devices currently registered in the hub.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<DeviceInfo>> GetAll()
    {
        return Ok(_registry.GetAll());
    }

    /// <summary>
    /// Returns a single device by its unique identifier, or 404 if no matching device is found.
    /// </summary>
    /// <param name="deviceId">The unique device identifier to look up.</param>
    [HttpGet("{deviceId}")]
    public ActionResult<DeviceInfo> GetById(string deviceId)
    {
        var device = _registry.GetById(deviceId);
        if (device is null) return NotFound();
        return Ok(device);
    }

    /// <summary>
    /// Registers a new device or overwrites an existing entry with the same DeviceId.
    /// Returns 201 Created with a Location header pointing to the new resource.
    /// </summary>
    /// <param name="device">Device metadata to register.</param>
    [HttpPost]
    public ActionResult<DeviceInfo> Register([FromBody] DeviceInfo device)
    {
        var registered = _registry.Register(device);
        return CreatedAtAction(nameof(GetById), new { deviceId = registered.DeviceId }, registered);
    }
}
