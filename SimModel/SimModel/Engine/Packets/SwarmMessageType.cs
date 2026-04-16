namespace Engine.Packets;

/// <summary>
/// Message type registry for the swarm protocol envelope.
/// </summary>
public enum SwarmMessageType : byte
{
    /// <summary>
    /// Device discovery request.
    /// </summary>
    FIND = 0x01,

    /// <summary>
    /// Discovery response.
    /// </summary>
    PONG = 0x02,

    /// <summary>
    /// Verification phase payload.
    /// </summary>
    VERIFY = 0x10,

    /// <summary>
    /// Protocol description request.
    /// </summary>
    PROTO = 0x11,

    /// <summary>
    /// Protocol description response.
    /// </summary>
    PROTO_R = 0x12,

    /// <summary>
    /// Session start message.
    /// </summary>
    START = 0x13,

    /// <summary>
    /// End-to-end acknowledgement.
    /// </summary>
    ACK = 0x20,

    /// <summary>
    /// Sleepy node pull request.
    /// </summary>
    PULL = 0x21,

    /// <summary>
    /// Sleepy node pull response.
    /// </summary>
    PULL_R = 0x22,

    /// <summary>
    /// Read output value request.
    /// </summary>
    IO_GET = 0x30,

    /// <summary>
    /// Read output value response.
    /// </summary>
    IO_GET_R = 0x31,

    /// <summary>
    /// Set input value request.
    /// </summary>
    IO_SET = 0x32,

    /// <summary>
    /// Set input value response.
    /// </summary>
    IO_SET_R = 0x33,

    /// <summary>
    /// Telemetry/event message.
    /// </summary>
    IO_EVENT = 0x34,

    /// <summary>
    /// Parent announcement and keep-alive hint.
    /// </summary>
    HELLO = 0x40,

    /// <summary>
    /// Wake-up broadcast hint.
    /// </summary>
    WAKE = 0x41,

    /// <summary>
    /// Gateway beacon with convergence hints.
    /// </summary>
    BEACON = 0x42,

    /// <summary>
    /// Charge decay broadcast.
    /// </summary>
    DECAY = 0x43,

    /// <summary>
    /// Fragment wrapper.
    /// </summary>
    FRAG = 0x7F,
}
