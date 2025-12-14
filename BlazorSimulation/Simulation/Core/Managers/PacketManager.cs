
using System.Numerics;

namespace Core.Managers;

public static class PacketManager
{
    public const float PACKET_SPEED = 300f; //px/s

    public static HashSet<Packet> ActivePackets { get; set; } = [];

    public static void RegisterPacket(Packet packet)
    {
        ActivePackets.Add(packet);
    }

    public static List<Vector2> TickPackets()
    {
        List<Vector2> renderData = [];
        var packetsCopy = ActivePackets.ToList();
        foreach (var p in packetsCopy)
        {
            if (p.CurrentHop is null || p.NextHop is null)
            {
                continue;
            }
            float distance = Vector2.Distance(p.CurrentHop.Pos, p.NextHop.Pos);
            if (distance < 1e-6f)
            {
                p.NextHop.HandlePacket(p);
                ActivePackets.Remove(p);
                continue;
            }
            float timeElapsed = (float)(DateTime.UtcNow - p.CreatedOn).TotalSeconds;
            float t = MathF.Min(timeElapsed * PACKET_SPEED / distance, 1f);
            if (t >= 1f - 1e-9)
            {
                p.NextHop.HandlePacket(p);
                ActivePackets.Remove(p);
                continue;
            }
            renderData.Add(Vector2.Lerp(p.CurrentHop.Pos, p.NextHop.Pos, t));
        }

        return renderData;
    }
}
