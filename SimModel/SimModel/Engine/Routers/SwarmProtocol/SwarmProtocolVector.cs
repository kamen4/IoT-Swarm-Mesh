namespace Engine.Routers;

/// <summary>
/// Runtime-tunable parameter vector for the swarm protocol implementation.
/// Values are aligned with the protocol tuning guide and Python batch model.
/// </summary>
public sealed class SwarmProtocolVector
{
    /// <summary>
    /// Forward-eligibility threshold for DOWN tree forwarding.
    /// </summary>
    public int QForward { get; set; } = 194;

    /// <summary>
    /// Root (gateway) source charge bootstrap value.
    /// </summary>
    public int RootSourceCharge { get; set; } = 1500;

    /// <summary>
    /// Absolute parent-switch hysteresis.
    /// </summary>
    public int SwitchHysteresis { get; set; } = 9;

    /// <summary>
    /// Relative parent-switch hysteresis.
    /// </summary>
    public double SwitchHysteresisRatio { get; set; } = 0.07;

    /// <summary>
    /// Parent expiration timeout in ticks.
    /// </summary>
    public int ParentDeadTicks { get; set; } = 120;

    /// <summary>
    /// Distance-based score penalty weight used in neighbor ranking.
    /// </summary>
    public double PenaltyLambda { get; set; } = 28.0;

    /// <summary>
    /// Expected charge attenuation per hop used in fallback checks.
    /// </summary>
    public int ChargeDropPerHop { get; set; } = 80;

    /// <summary>
    /// Charge estimate blending factor in range [0.01, 1].
    /// </summary>
    public double ChargeSpreadFactor { get; set; } = 0.28;

    /// <summary>
    /// Decay interval in ticks (0 disables decay).
    /// </summary>
    public int DecayIntervalSteps { get; set; } = 60;

    /// <summary>
    /// Decay factor in range [0, 0.8].
    /// </summary>
    public double DecayPercent { get; set; } = 0.13;

    /// <summary>
    /// Link-quality memory coefficient in range [0.6, 0.999].
    /// </summary>
    public double LinkMemory { get; set; } = 0.912;

    /// <summary>
    /// Link-quality adaptation speed in range [0.01, 2].
    /// </summary>
    public double LinkLearningRate { get; set; } = 0.23;

    /// <summary>
    /// Maximum additive parent-score bonus derived from learned link quality.
    /// </summary>
    public int LinkBonusMax { get; set; } = 60;

    /// <summary>
    /// Creates a deep copy of this vector.
    /// </summary>
    public SwarmProtocolVector Clone()
        => (SwarmProtocolVector)MemberwiseClone();

    /// <summary>
    /// Creates a normalized copy clamped to safe runtime bounds.
    /// </summary>
    public SwarmProtocolVector Normalized()
    {
        var copy = Clone();
        copy.NormalizeInPlace();
        return copy;
    }

    /// <summary>
    /// Clamps all fields to safe runtime bounds.
    /// </summary>
    public void NormalizeInPlace()
    {
        QForward = Math.Clamp(QForward, 20, 1800);
        RootSourceCharge = Math.Clamp(RootSourceCharge, 250, 3000);
        SwitchHysteresis = Math.Clamp(SwitchHysteresis, 0, 260);
        SwitchHysteresisRatio = Math.Clamp(SwitchHysteresisRatio, 0.0, 0.40);
        ParentDeadTicks = Math.Clamp(ParentDeadTicks, 1, 10_000);
        PenaltyLambda = Math.Clamp(PenaltyLambda, 0.0, 250.0);
        ChargeDropPerHop = Math.Clamp(ChargeDropPerHop, 5, 420);
        ChargeSpreadFactor = Math.Clamp(ChargeSpreadFactor, 0.01, 1.0);
        DecayIntervalSteps = Math.Clamp(DecayIntervalSteps, 0, 2000);
        DecayPercent = Math.Clamp(DecayPercent, 0.0, 0.80);
        LinkMemory = Math.Clamp(LinkMemory, 0.600, 0.999);
        LinkLearningRate = Math.Clamp(LinkLearningRate, 0.01, 2.00);
        LinkBonusMax = Math.Clamp(LinkBonusMax, 0, 240);
    }
}
