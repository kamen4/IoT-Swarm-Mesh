using Core.Devices;

namespace Core.Contracts;

public interface IHandler
{
    void Handle(Device device, Packet packet);
}
