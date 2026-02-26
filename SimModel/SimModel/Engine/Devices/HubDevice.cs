using Engine.Packets;

namespace Engine.Devices;

public class HubDevice : Device
{
    public override void Accept(Packet packet)
    {
        Log(packet);
    }

    private void Log(Packet packet)
    {
    }
}
