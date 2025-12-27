using Core.Devices;
using Core.Services;

namespace Core;

public class Packet
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid IdempotencyId { get; set; } = Guid.NewGuid();
    public PacketType PacketType { get; set; } = PacketType.Ping;
    
    public Device Sender { get; set; } = null!;
    public Device? Receiver { get; set; }
    public byte[]? Payload { get; set; }
    
    public bool ConfirmDelivery { get; set; } = true;
    public bool DirectionForward { get; set; } = true;
    
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime OriginalCreatedOn { get; set; } = DateTime.UtcNow;
    public Device? CurrentHop { get; set; }
    public Device? NextHop { get; set; }
    
    public int TTL { get; set; } = 64;
    public int HopCount { get; set; } = 0;
    
    /// <summary>
    /// Пакет ещё не начал движение (ждёт обработки на sender)
    /// </summary>
    public bool IsInitial { get; set; } = true;
    
    public object? HandlerData { get; set; }

    public Packet()
    {
    }

    public Packet(Device sender, Device? receiver)
    {
        Sender = sender;
        CurrentHop = sender;
        NextHop = sender; // Изначально пакет на sender, обработчик решит куда отправить
        Receiver = receiver;
        IsInitial = true;
    }

    public Packet RemakeForNextHop(Device hop)
    {
        return new Packet
        {
            IdempotencyId = IdempotencyId,
            PacketType = PacketType,
            Sender = Sender,
            Receiver = Receiver,
            Payload = Payload,
            ConfirmDelivery = ConfirmDelivery,
            DirectionForward = DirectionForward,
            CurrentHop = NextHop,
            NextHop = hop,
            TTL = TTL - 1,
            HopCount = HopCount + 1,
            CreatedOn = DateTime.UtcNow,
            OriginalCreatedOn = OriginalCreatedOn,
            IsInitial = false
        };
    }

    public override int GetHashCode() => Id.GetHashCode();
    
    public override bool Equals(object? obj) => obj is Packet p && p.Id == Id;
}