using Engine.Devices;

namespace Engine.Packets;

/// <summary>
/// A special packet that is automatically generated and sent back to the
/// original sender when a packet with <see cref="Packet.NeedConfirmation"/>
/// set to <c>true</c> is successfully delivered.
/// The payload contains the <see cref="Guid"/> bytes of the confirmed packet,
/// allowing the sender to correlate the acknowledgement with the original
/// transmission.
/// </summary>
public class ConfirmationPacket : Packet
{
    /// <summary>
    /// Initialises a new confirmation packet as a reply to <paramref name="original"/>.
    /// The source and destination are swapped — the confirmation travels from
    /// the original destination back to the original sender.
    /// </summary>
    /// <param name="original">The packet whose delivery is being acknowledged.</param>
    public ConfirmationPacket(Packet original)
        : base(original.To, original.From, new()
        {
            Data = original.Id.ToByteArray()
        })
    {
    }
}
