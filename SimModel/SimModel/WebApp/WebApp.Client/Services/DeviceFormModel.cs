namespace WebApp.Client.Services;

/// <summary>Discriminates the type of device being created or edited in the Device modal.</summary>
public enum DeviceType { Hub, Generator, Emitter }

/// <summary>
/// View-model for the Add / Edit Device modal.
/// Aggregates all editable device properties into a single flat model;
/// <see cref="SimulationService.AddDevice"/> and
/// <see cref="SimulationService.UpdateDevice"/> map it back to the appropriate
/// <see cref="Engine.Devices.Device"/> subclass.
/// </summary>
public class DeviceFormModel
{
    /// <summary>Display name entered by the user for the device.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The type of device to create; determines which
    /// <see cref="Engine.Devices.Device"/> subclass is constructed.
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.Generator;

    /// <summary>X coordinate of the device on the 2D simulation plane.</summary>
    public float X { get; set; }

    /// <summary>Y coordinate of the device on the 2D simulation plane.</summary>
    public float Y { get; set; }

    /// <summary>
    /// Packet generation interval in ticks, used when <see cref="DeviceType"/> is
    /// <see cref="DeviceType.Generator"/>.
    /// Maps to <see cref="Engine.Devices.GeneratorDevice.GenFrequencyTicks"/>.
    /// </summary>
    public long GenFrequencyTicks { get; set; } = 40;

    /// <summary>
    /// Hub-to-emitter control interval in ticks, used when <see cref="DeviceType"/> is
    /// <see cref="DeviceType.Emitter"/>.
    /// Maps to <see cref="Engine.Devices.EmitterDevice.ControlFrequencyTicks"/>.
    /// Set to <c>0</c> to disable automatic control packets.
    /// </summary>
    public long ControlFrequencyTicks { get; set; } = 0;
}
