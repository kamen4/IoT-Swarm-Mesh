using System.Numerics;
using System.Text;
using Core.Services;

namespace Core.Devices;

public abstract class Device : ICloneable
{
    public enum PowerType
    {
        Battery,
        AC,
    }

    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public double Battery { get; set; } = 1.0;
    public PowerType DevicePowerType { get; set; } = PowerType.Battery;
    public double Radius { get; set; } = 50;
    public double BatteryDrainRate { get; set; } = 1.0;

    public Device()
    {
        Id = Guid.NewGuid();
    }

    public Device(Guid id)
    {
        Id = id;   
    }

    public List<Device> Connections { get; set; } = [];

    public override bool Equals(object? obj)
    {
        if (obj is Device d)
        {
            return Id.Equals(d.Id);
        }
        return base.Equals(obj);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public Vector2 Pos { get; set; } = new();

    public abstract string Color { get; }
    public abstract int SizeR { get; }

    private readonly HashSet<Guid> _processedPackets = [];
    
    public virtual void AcceptPacket(Packet packet)
    {
        if (_processedPackets.Contains(packet.IdempotencyId))
        {
            return;
        }
        
        _processedPackets.Add(packet.IdempotencyId);
        
        // Limit memory - keep only last 1000 packets
        if (_processedPackets.Count > 1000)
        {
            var oldest = _processedPackets.Take(100).ToList();
            foreach (var id in oldest)
            {
                _processedPackets.Remove(id);
            }
        }

        OnPacketAccepted(packet);
    }

    protected virtual void OnPacketAccepted(Packet packet)
    {
        // Override in derived classes for specific behavior
    }

    public void ClearProcessedPackets()
    {
        _processedPackets.Clear();
    }

    public virtual object Clone()
    {
        var clone = (Device)MemberwiseClone();
        clone.Id = Guid.NewGuid();
        clone.Connections = [];
        return clone;
    }
}