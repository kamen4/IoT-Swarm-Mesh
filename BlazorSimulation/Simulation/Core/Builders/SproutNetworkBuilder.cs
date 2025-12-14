using System.Text;
using System.Text.Unicode;
using Core.Contracts;
using Core.Devices;
using Core.Managers;

namespace Core.Builders;

public class SproutNetworkBuilder : INetworkBuilder
{
    private const int CHECK_DELAY_MILLIS = 1500;
    private const int RETRY_COUNT = 10;

    public void AcceptBuildPacket(Device device, Packet packet)
    {
        if (packet.PacketType != Packet.Type.NetworkBuild)
        {
            return;
        }

        var toConnectId = new Guid(packet.Payload!);

        if (device.Connections.Any(d => d.Id == toConnectId))
        {
            //Packet p = new(device, packet.Sender)
            //{
            //    PacketType = Packet.Type.NetworkBuild,
            //    Payload = Encoding.UTF8.GetBytes($"ACK {toConnectId}")
            //};
            return;
        }

        var toConnectDevice = DeviceManager.GetVisibilitiesForDevice(device).FirstOrDefault(d => d.Id == toConnectId);
        if (toConnectDevice is not null)
        {
            device.Connections.Add(toConnectDevice);
            toConnectDevice.Connections.Add(device);

            //Packet p = new(device, packet.Sender)
            //{
            //    PacketType = Packet.Type.NetworkBuild,
            //    Payload = Encoding.UTF8.GetBytes($"ACK {toConnectId}")
            //};
            return;
        }
    }

    public async Task Build(Hub hub, List<Device> allDevices)
    {
        hub.Connections = DeviceManager.GetVisibilitiesForDevice(hub).ToList();
        var toConnect = allDevices.Except(hub.Connections).ToList();
        foreach (var d in toConnect)
        {
            Packet p = new(hub, null)
            {
                PacketType = Packet.Type.NetworkBuild,
                Payload = d.Id.ToByteArray()
            };
        }

        int tries = 0;
        while (++tries < RETRY_COUNT)
        { 
            await Task.Delay(CHECK_DELAY_MILLIS);
            var notConnected = CheckBuild(hub, allDevices);
            if (notConnected.Count == 0)
            {
                Console.WriteLine("Network build complete.");
                return;
            }
            else
            {
                Console.WriteLine($"Network build incomplete. {notConnected.Count} devices not connected. Retrying...");
                foreach (var d in notConnected)
                {
                    Packet p = new(hub, null)
                    {
                        PacketType = Packet.Type.NetworkBuild,
                        Payload = d.Id.ToByteArray()
                    };
                }
            }
        }
    }

    private static List<Device> CheckBuild(Hub hub, List<Device> allDevices)
    {
        HashSet<Device> connected = [hub];
        foreach(var d in hub.Connections)
        {
            connected.Add(d);
        }
        Stack<Device> st = new(connected);
        while (st.Count > 0)
        {
            var curD = st.Pop();
            foreach (var d in curD.Connections)
            {
                if (!connected.Contains(d))
                {
                    st.Push(d);
                    connected.Add(d);
                }
            }
        }

        return allDevices.Except(connected).ToList();
    }
}
