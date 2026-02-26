namespace WebApp.Client.Services;

public enum DeviceType { Hub, Generator, Emitter }

public class DeviceFormModel
{
    public string Name { get; set; } = "";
    public DeviceType DeviceType { get; set; } = DeviceType.Generator;
    public float X { get; set; }
    public float Y { get; set; }
    public long GenFrequencyTicks { get; set; } = 40;
}
