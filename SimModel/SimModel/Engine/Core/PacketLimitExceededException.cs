namespace Engine.Core;

/// <summary>
/// Thrown by <see cref="SimulationEngine.RegisterPacket"/> when the number of
/// in-flight packets in the priority queue reaches or exceeds
/// <see cref="SimulationEngine.MaxActivePackets"/>.
/// <para>
/// This exception signals that the simulation has entered an unstable state
/// (packet storm / routing loop) where more packets are being generated than
/// the network can drain. The host should catch this exception, stop the tick
/// loop, and surface the error to the user.
/// </para>
/// </summary>
public sealed class PacketLimitExceededException : Exception
{
    /// <summary>
    /// Gets the configured maximum number of simultaneously in-flight packets
    /// that was exceeded.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// Gets the actual number of in-flight packets at the moment the limit was
    /// exceeded.
    /// </summary>
    public int Actual { get; }

    /// <summary>
    /// Gets the engine tick at which the limit was exceeded.
    /// </summary>
    public long TickCount { get; }

    /// <summary>
    /// Initialises a new <see cref="PacketLimitExceededException"/>.
    /// </summary>
    /// <param name="limit">The configured packet limit.</param>
    /// <param name="actual">The actual in-flight packet count at the moment of violation.</param>
    /// <param name="tickCount">The engine tick at which the limit was exceeded.</param>
    public PacketLimitExceededException(int limit, int actual, long tickCount)
        : base($"Active-packet limit exceeded at tick {tickCount}: {actual} packets in flight (limit {limit}). " +
               "Possible causes: routing loop, packet storm, or limit set too low.")
    {
        Limit      = limit;
        Actual     = actual;
        TickCount  = tickCount;
    }
}
