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
    
    public override string Color => IsLampOn ? "#c2ae1b" : "#dddddd";
    public override int SizeR => 15;

    public override void AcceptPacket(Packet packet)
    {
        base.AcceptPacket(packet);
    }
}
