
namespace Core.Devices;

public class Lamp : Device
{
    public bool IsLampOn { get; set; } = true;

    public Lamp(Guid id) : base(id)
    {
    }

    public Lamp()
    {
    }
    
    public override string Color => "#c2ae1b";
    public override int SizeR => 15;

    public override void AcceptPacket(Packet packet)
    {
        base.AcceptPacket(packet); // Call base logic first
        // Lamp-specific packet handling can go here if needed
    }
}
