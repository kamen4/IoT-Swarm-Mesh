using Core.Services;

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
    
    public override string Color => IsLampOn ? "#c2ae1b" : "#888888";
    public override int SizeR => 15;

    protected override void OnPacketAccepted(Packet packet)
    {
        if (packet.PacketType == PacketType.LampCommand && packet.Payload?.Length >= 1)
        {
            IsLampOn = packet.Payload[0] == 1;
        }
    }
}
