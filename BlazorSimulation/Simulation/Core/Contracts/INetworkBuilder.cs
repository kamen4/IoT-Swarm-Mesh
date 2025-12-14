using Core.Devices;

namespace Core.Contracts;

public interface INetworkBuilder
{
    public Task Build(Hub hub, List<Device> allDevices);
    public void AcceptBuildPacket(Device device, Packet packet);
}
