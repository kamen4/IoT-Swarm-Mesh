namespace Core.Devices;

public class Hub : Device
{
    public Hub(Guid id) : base(id)
    {
        DevicePowerType = PowerType.AC;
    }

    public Hub()
    {
        DevicePowerType = PowerType.AC;
    }

    public override string Color => "#7337bd";
    public override int SizeR => 20;
}
