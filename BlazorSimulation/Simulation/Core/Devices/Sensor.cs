namespace Core.Devices;

public class Sensor : Device
{
    private readonly Random _random;
    
    public double LastValue { get; private set; }

    public Sensor(Guid id) : base(id)
    {
        _random = new Random(id.GetHashCode());
    }

    public Sensor()
    {
        _random = new Random(Id.GetHashCode());
    }

    public double GenData()
    {
        LastValue = _random.NextDouble() * 100; // 0-100 range
        return LastValue;
    }

    public override string Color => "#299450";
    public override int SizeR => 10;
}