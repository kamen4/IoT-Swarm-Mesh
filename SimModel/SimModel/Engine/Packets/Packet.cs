using Engine.Devices;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Engine.Packets;

/// <summary>
/// Represents a unit of data travelling through the simulation network.
/// A packet knows its originator (<see cref="From"/>), its intended
/// destination (<see cref="To"/>), and the <see cref="NextHop"/> device it
/// will be delivered to on the next simulation tick.
/// Travel time is expressed in engine ticks via <see cref="TicksToTravel"/>,
/// and each hop decrements the <see cref="TTL"/> counter - a packet that
/// reaches zero TTL is silently dropped.
/// <para>
/// The same class also carries a swarm protocol envelope:
/// <list type="bullet">
///   <item>Routing metadata: version, previous-hop MAC, advertised charge and decay hint.</item>
///   <item>Secure header fields: direction, message type, origin/destination MAC, message id and sequence.</item>
///   <item>End-to-end tag bytes used by HMAC-SHA256 truncation.</item>
/// </list>
/// These fields are optional for legacy routing and are populated by default
/// for newly created packets.
/// </para>
/// </summary>
public class Packet
{
    /// <summary>
    /// Protocol envelope version currently emitted by this simulator.
    /// </summary>
    public const byte PROTOCOL_VERSION = 1;

    /// <summary>
    /// Library default TTL used by packet constructors.
    /// </summary>
    public const int DEFAULT_TTL = 10;

    /// <summary>
    /// Library default per-hop travel time used by packet constructors.
    /// </summary>
    public const long DEFAULT_TICKS_TO_TRAVEL = 3;

    /// <summary>
    /// Truncated authentication tag length in bytes.
    /// </summary>
    public const int TAG_LENGTH = 16;

    /// <summary>
    /// Initialises a new packet.
    /// <see cref="OriginId"/> is set to the same value as <see cref="Id"/> so
    /// that all flood clones produced by <see cref="Clone"/> inherit the
    /// original packet's identity.
    /// </summary>
    public Packet(Device from, Device to, PacketData payload)
    {
        From     = from;
        To       = to;
        Payload  = payload;
        Id       = Guid.NewGuid();
        OriginId = Id; // clones will keep this value via MemberwiseClone

        Version        = PROTOCOL_VERSION;
        Direction      = from is HubDevice ? PacketDirection.Down : PacketDirection.Up;
        MessageType    = SwarmMessageType.IO_EVENT;
        OriginMac      = PacketAddress.Clone(from.MacAddress);
        DestinationMac = PacketAddress.Clone(to.MacAddress);
        PreviousHopMac = PacketAddress.Clone(PacketAddress.Empty);
        MessageId      = from.AllocateMessageId();
        Sequence       = from.AllocateSequence();
        Tag            = new byte[TAG_LENGTH];
    }

    /// <summary>Gets the unique identifier of this packet instance.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the identifier shared by this packet and all of its flood clones.
    /// On the original packet <c>OriginId == Id</c>.
    /// After <see cref="Clone"/>, every copy keeps the same <see cref="OriginId"/>
    /// because <c>MemberwiseClone</c> copies the field as-is.
    /// <para>
    /// <see cref="Statistics.SimulationStatistics"/> uses this to detect duplicate
    /// deliveries: when a second clone with the same <see cref="OriginId"/> reaches
    /// the destination it is counted as a duplicate rather than a new delivery.
    /// </para>
    /// </summary>
    public Guid OriginId { get; }

    /// <summary>
    /// Gets or sets the protocol envelope version.
    /// </summary>
    public byte Version { get; set; }

    /// <summary>
    /// Gets or sets the secure-header direction field.
    /// </summary>
    public PacketDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the secure-header message type field.
    /// </summary>
    public SwarmMessageType MessageType { get; set; }

    /// <summary>
    /// Gets or sets the secure-header origin MAC address (6 bytes).
    /// </summary>
    public byte[] OriginMac { get; set; }

    /// <summary>
    /// Gets or sets the secure-header destination MAC address (6 bytes).
    /// </summary>
    public byte[] DestinationMac { get; set; }

    /// <summary>
    /// Gets or sets the routing-header previous-hop MAC address (6 bytes).
    /// </summary>
    public byte[] PreviousHopMac { get; set; }

    /// <summary>
    /// Gets or sets the secure-header message id.
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// Gets or sets the secure-header sequence value.
    /// </summary>
    public ushort Sequence { get; set; }

    /// <summary>
    /// Gets or sets the routing-header advertised charge value.
    /// </summary>
    public ushort AdvertisedCharge { get; set; }

    /// <summary>
    /// Gets or sets the routing-header decay epoch hint.
    /// </summary>
    public ushort DecayEpochHint { get; set; }

    /// <summary>
    /// Gets or sets the optional fragment-group id.
    /// </summary>
    public ushort? FragmentGroupId { get; set; }

    /// <summary>
    /// Gets or sets the optional fragment index.
    /// </summary>
    public byte? FragmentIndex { get; set; }

    /// <summary>
    /// Gets or sets the optional total number of fragments.
    /// </summary>
    public byte? FragmentCount { get; set; }

    /// <summary>
    /// Gets the current authentication tag bytes.
    /// </summary>
    public byte[] Tag { get; private set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="DestinationMac"/> is the
    /// broadcast address.
    /// </summary>
    public bool IsBroadcastDestination => PacketAddress.IsBroadcast(DestinationMac);

    /// <summary>
    /// Gets the TTL value that was recorded when this packet was first enqueued
    /// by <see cref="Engine.Core.SimulationEngine.RegisterPacket"/>.
    /// <para>
    /// Set once on the first enqueue; clones inherit it through
    /// <c>MemberwiseClone</c> so every clone knows the starting TTL.
    /// Used by <see cref="Statistics.SimulationStatistics"/> to compute
    /// hop count at delivery: <c>hops = InitialTtl - TTL</c>.
    /// </para>
    /// </summary>
    public int InitialTtl { get; internal set; }

    /// <summary>
    /// Gets or sets the number of engine ticks the packet takes to travel
    /// between two adjacent devices.
    /// </summary>
    public long TicksToTravel { get; set; } = DEFAULT_TICKS_TO_TRAVEL;

    /// <summary>
    /// Gets or sets the absolute engine tick at which this packet is due to
    /// arrive at <see cref="NextHop"/>.
    /// Set by <see cref="Engine.Core.SimulationEngine.RegisterPacket"/> when the packet
    /// is enqueued.
    /// </summary>
    public long ArrivalTick { get; set; } = 0;

    /// <summary>Gets or sets the device that will receive this packet on arrival.</summary>
    public Device NextHop { get; set; } = null!;

    /// <summary>
    /// Gets or sets the device that forwarded this packet to <see cref="NextHop"/>
    /// on the previous hop.
    /// <para>
    /// Set by <see cref="Engine.Devices.Device.Recieve"/> just before the packet is
    /// re-routed, so that the active <see cref="Engine.Routers.IPacketRouter"/> can
    /// skip sending the packet back to the node it just came from.
    /// <c>null</c> on the first hop (packet originates directly from <see cref="From"/>).
    /// </para>
    /// </summary>
    public Device? PreviousHop { get; set; } = null;

    /// <summary>
    /// Gets or sets the time-to-live counter. Decremented on every hop;
    /// the packet is dropped when it reaches zero.
    /// </summary>
    public int TTL { get; set; } = DEFAULT_TTL;

    /// <summary>Gets the device that originally sent this packet.</summary>
    public Device From { get; }

    /// <summary>Gets the intended destination device for this packet.</summary>
    public Device To { get; }

    /// <summary>Gets the data payload carried by this packet.</summary>
    public PacketData Payload { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the destination device should
    /// send a <see cref="ConfirmationPacket"/> back to <see cref="From"/>
    /// upon successful delivery.
    /// </summary>
    public bool NeedConfirmation { get; set; } = false;

    /// <summary>
    /// Creates a shallow copy of this packet, suitable for broadcasting the
    /// same packet to multiple next-hop neighbours.
    /// All clones share the same <see cref="OriginId"/> as the original.
    /// Address and tag arrays are duplicated so clones can mutate envelope
    /// metadata independently.
    /// </summary>
    /// <returns>A shallow clone of this packet.</returns>
    public Packet Clone()
    {
        var clone = (MemberwiseClone() as Packet)!;
        clone.OriginMac      = PacketAddress.Clone(OriginMac);
        clone.DestinationMac = PacketAddress.Clone(DestinationMac);
        clone.PreviousHopMac = PacketAddress.Clone(PreviousHopMac);
        clone.Tag            = (byte[])Tag.Clone();
        return clone;
    }

    /// <summary>
    /// Sets <see cref="DestinationMac"/> to the broadcast address.
    /// </summary>
    public void MarkBroadcastDestination()
    {
        DestinationMac = PacketAddress.Clone(PacketAddress.Broadcast);
    }

    /// <summary>
    /// Updates <see cref="Tag"/> by computing HMAC-SHA256 over
    /// secure-header bytes plus payload bytes, then truncating to 16 bytes.
    /// Routing-header bytes are intentionally excluded.
    /// </summary>
    /// <param name="sharedSecret">Shared secret bytes.</param>
    public void UpdateTag(ReadOnlySpan<byte> sharedSecret)
    {
        if (sharedSecret.Length == 0)
        {
            Array.Clear(Tag);
            return;
        }

        var secureFrame = BuildSecureHeaderAndPayload();
        using var hmac = new HMACSHA256(sharedSecret.ToArray());
        var hash = hmac.ComputeHash(secureFrame);

        Tag = new byte[TAG_LENGTH];
        Array.Copy(hash, 0, Tag, 0, TAG_LENGTH);
    }

    /// <summary>
    /// Validates <see cref="Tag"/> against the current packet fields using
    /// the provided shared secret.
    /// </summary>
    /// <param name="sharedSecret">Shared secret bytes.</param>
    /// <returns><c>true</c> when the computed tag matches.</returns>
    public bool ValidateTag(ReadOnlySpan<byte> sharedSecret)
    {
        if (Tag.Length != TAG_LENGTH)
            return false;

        if (sharedSecret.Length == 0)
            return Tag.All(static b => b == 0);

        var secureFrame = BuildSecureHeaderAndPayload();
        using var hmac = new HMACSHA256(sharedSecret.ToArray());
        var hash = hmac.ComputeHash(secureFrame);

        for (var i = 0; i < TAG_LENGTH; i++)
            if (Tag[i] != hash[i])
                return false;

        return true;
    }

    /// <summary>
    /// Builds a byte buffer consisting of secure-header fields followed by
    /// serialized payload bytes.
    /// </summary>
    /// <returns>Serialized secure-header + payload bytes.</returns>
    public byte[] BuildSecureHeaderAndPayload()
    {
        var payloadBytes = SerializePayload(Payload.Data);
        var bytes = new byte[18 + payloadBytes.Length];

        var offset = 0;
        bytes[offset++] = (byte)Direction;
        bytes[offset++] = (byte)MessageType;

        Array.Copy(OriginMac, 0, bytes, offset, PacketAddress.AddressLength);
        offset += PacketAddress.AddressLength;

        Array.Copy(DestinationMac, 0, bytes, offset, PacketAddress.AddressLength);
        offset += PacketAddress.AddressLength;

        WriteUInt16LittleEndian(bytes, ref offset, MessageId);
        WriteUInt16LittleEndian(bytes, ref offset, Sequence);

        Array.Copy(payloadBytes, 0, bytes, offset, payloadBytes.Length);
        return bytes;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is Packet p)
            return Id.Equals(p.Id);
        return base.Equals(obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();

    private static void WriteUInt16LittleEndian(byte[] bytes, ref int offset, ushort value)
    {
        bytes[offset++] = (byte)(value & 0xFF);
        bytes[offset++] = (byte)((value >> 8) & 0xFF);
    }

    private static byte[] SerializePayload(object? value)
    {
        return value switch
        {
            null        => [],
            byte[] data => data.ToArray(),
            bool flag   => [flag ? (byte)1 : (byte)0],
            byte b      => [b],
            sbyte sb    => [(byte)sb],
            short s     => BitConverter.GetBytes(s),
            ushort us   => BitConverter.GetBytes(us),
            int i       => BitConverter.GetBytes(i),
            uint ui     => BitConverter.GetBytes(ui),
            long l      => BitConverter.GetBytes(l),
            ulong ul    => BitConverter.GetBytes(ul),
            float f     => BitConverter.GetBytes(f),
            double d    => BitConverter.GetBytes(d),
            Guid g      => g.ToByteArray(),
            DateTime dt => BitConverter.GetBytes(dt.ToBinary()),
            decimal m   => SerializeDecimal(m),
            string str  => Encoding.UTF8.GetBytes(str),
            IFormattable formattable
                => Encoding.UTF8.GetBytes(formattable.ToString(null, CultureInfo.InvariantCulture)),
            _ => Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty),
        };
    }

    private static byte[] SerializeDecimal(decimal value)
    {
        var bits = decimal.GetBits(value);
        var bytes = new byte[16];
        Buffer.BlockCopy(bits, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}