namespace Common.Entities;

/// <summary>Stores registration metadata for an IoT device tracked by the hub.</summary>
public class DeviceInfo
{
    /// <summary>Unique identifier for the device, used as the primary key in the registry.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Human-readable display name assigned to the device.</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>Category or model descriptor indicating the kind of device (e.g., relay, button, gateway).</summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>Indicates whether the device is currently reachable on the mesh network.</summary>
    public bool IsOnline { get; set; }
}
