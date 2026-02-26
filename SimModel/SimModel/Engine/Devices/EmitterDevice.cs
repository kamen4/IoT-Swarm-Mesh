using Engine.Packets;

namespace Engine.Devices;

public class EmitterDevice : Device
{
    public bool State { get; protected set; }
    public override void Accept(Packet packet)
    {
        if (packet.Payload.Data is bool state)
        {
            State = state;
        }
    }
}
