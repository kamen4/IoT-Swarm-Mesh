namespace Core.Devices;

public class Sensor : Device
{
    public Sensor(Guid id) : base(id)
    {
    }

    public Sensor()
    {
    }

    public double GenData()
    {
        return new Random(Id.GetHashCode()).NextDouble();
    }

    public override string Color => "#299450";
    public override int SizeR => 10;

    public override void AcceptPacket(Packet packet)
    {
        base.AcceptPacket(packet); // Call base logic first
        // Sensor-specific packet handling can go here if needed
        if (packet.PacketType == Packet.Type.Data)
        {
            // Example specific logic
        }
    }
}