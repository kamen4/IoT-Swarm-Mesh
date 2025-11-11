
namespace Core.Devices;

public class Hub : Device
{
    public Hub(Guid id) : base(id)
    {
    }

    public Hub()
    {
    }


    public override string Color => "#7337bd";
    public override int SizeR => 20;
}
