using System.Text;

namespace Engine.Packets;

/// <summary>
/// Helpers for fixed-width 6-byte mesh addresses used by protocol headers.
/// </summary>
public static class PacketAddress
{
    /// <summary>
    /// Address length in bytes.
    /// </summary>
    public const int AddressLength = 6;

    /// <summary>
    /// Broadcast mesh address.
    /// </summary>
    public static readonly byte[] Broadcast =
    [
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
    ];

    /// <summary>
    /// All-zero address used as an empty previous-hop marker.
    /// </summary>
    public static readonly byte[] Empty =
    [
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
    ];

    /// <summary>
    /// Creates a locally-administered unicast mesh address from a GUID.
    /// </summary>
    /// <param name="value">Source GUID.</param>
    /// <returns>A 6-byte address derived from the GUID bytes.</returns>
    public static byte[] FromGuid(Guid value)
    {
        var source = value.ToByteArray();
        var mac = new byte[AddressLength];
        Array.Copy(source, 0, mac, 0, AddressLength);

        // Set local bit and clear multicast bit.
        mac[0] |= 0x02;
        mac[0] &= 0xFE;

        return mac;
    }

    /// <summary>
    /// Returns a defensive copy of an address.
    /// </summary>
    /// <param name="value">Address bytes.</param>
    /// <returns>Copied address with exactly 6 bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when length is not 6 bytes.</exception>
    public static byte[] Clone(ReadOnlySpan<byte> value)
    {
        if (value.Length != AddressLength)
            throw new ArgumentException("Address must be exactly 6 bytes.", nameof(value));

        var copy = new byte[AddressLength];
        value.CopyTo(copy);
        return copy;
    }

    /// <summary>
    /// Compares two addresses for byte-wise equality.
    /// </summary>
    /// <param name="left">Left address.</param>
    /// <param name="right">Right address.</param>
    /// <returns><c>true</c> when both addresses are 6 bytes and equal.</returns>
    public static bool EqualsMac(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != AddressLength || right.Length != AddressLength)
            return false;

        return left.SequenceEqual(right);
    }

    /// <summary>
    /// Compares two addresses lexicographically.
    /// </summary>
    /// <param name="left">Left address.</param>
    /// <param name="right">Right address.</param>
    /// <returns>
    /// Negative when left is smaller, zero when equal, positive when left is greater.
    /// </returns>
    public static int Compare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var min = Math.Min(left.Length, right.Length);
        for (var i = 0; i < min; i++)
        {
            if (left[i] == right[i])
                continue;

            return left[i] < right[i] ? -1 : 1;
        }

        if (left.Length == right.Length)
            return 0;

        return left.Length < right.Length ? -1 : 1;
    }

    /// <summary>
    /// Returns <c>true</c> when the address is the broadcast value.
    /// </summary>
    /// <param name="value">Address bytes to test.</param>
    /// <returns><c>true</c> for FF:FF:FF:FF:FF:FF.</returns>
    public static bool IsBroadcast(ReadOnlySpan<byte> value)
        => EqualsMac(value, Broadcast);

    /// <summary>
    /// Converts an address to uppercase hex text without separators.
    /// Useful as dictionary keys.
    /// </summary>
    /// <param name="value">Address bytes.</param>
    /// <returns>Hex key text.</returns>
    public static string ToKey(ReadOnlySpan<byte> value)
    {
        var builder = new StringBuilder(AddressLength * 2);
        foreach (var b in value)
            builder.Append(b.ToString("X2"));

        return builder.ToString();
    }
}
