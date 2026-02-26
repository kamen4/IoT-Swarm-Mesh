using Engine.Devices;

namespace Engine.Packets;

public class ConfirmationPacket : Packet
{
    public ConfirmationPacket(Packet original) 
        : base(original.To, original.From, new()
        {
            Data = original.Id.ToByteArray()
        })
    {
    }
}
