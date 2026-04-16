namespace Engine.Packets;

/// <summary>
/// Payload for BEACON messages.
/// </summary>
/// <param name="RecommendedForwardThreshold">
/// Suggested q_forward threshold distributed by the gateway.
/// </param>
public sealed record BeaconPayload(ushort RecommendedForwardThreshold);

/// <summary>
/// Payload for DECAY messages.
/// </summary>
/// <param name="DecayEpoch">Monotonic decay epoch number.</param>
/// <param name="DecayPercent">Decay percent in range [0, 1].</param>
public sealed record DecayPayload(ushort DecayEpoch, double DecayPercent);
