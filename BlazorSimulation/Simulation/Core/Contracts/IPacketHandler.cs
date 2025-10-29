namespace Core.Contracts;

public interface IPacketHandler
{
    void Handle(Device device, Packet packet);
}
