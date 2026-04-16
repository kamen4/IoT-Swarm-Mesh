using Engine.Core;
using Engine.Packets;
using Engine.Routers;
using System.Globalization;
using System.Numerics;

namespace Engine.Devices;

/// <summary>
/// Abstract base class for all devices in the simulation network.
/// A device has a unique identity and a position in 2D space.
/// When it receives a packet it either forwards it (if it is not the
/// intended recipient) or accepts and processes it.
/// <para>
/// Forwarding is delegated to <see cref="SimulationEngine.RoutePacket"/> so that
/// the active <see cref="Engine.Routers.IPacketRouter"/> is always consulted.
/// Devices never reference a concrete router directly.
/// </para>
/// </summary>
public abstract class Device
{
    private readonly Dictionary<string, NeighborState> _neighbors = [];
    private readonly HashSet<string> _dedupCache = new(StringComparer.Ordinal);
    private readonly Queue<string> _dedupOrder = new();

    private ushort _nextMessageId = 1;
    private ushort _nextSequence = 1;

    /// <summary>
    /// Initializes protocol defaults shared by all devices.
    /// </summary>
    protected Device()
    {
        MacAddress = PacketAddress.FromGuid(Id);
    }

    /// <summary>Gets or sets the human-readable name of this device.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gets the unique identifier of this device.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the 6-byte mesh address of this device.
    /// </summary>
    public byte[] MacAddress { get; }

    /// <summary>Gets or sets the 2D position of this device in the simulation world.</summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of dedup entries retained locally.
    /// </summary>
    public int DedupCacheCapacity { get; set; } = 512;

    /// <summary>
    /// Gets or sets the minimum total charge required for forward eligibility.
    /// </summary>
    public ushort QForwardThreshold { get; set; } = 100;

    /// <summary>
    /// Gets or sets the max parent age in ticks before parent state is cleared.
    /// </summary>
    public int ParentDeadTicks { get; set; } = 60;

    /// <summary>
    /// Gets or sets the absolute charge delta required to switch parent.
    /// </summary>
    public ushort ParentSwitchHysteresis { get; set; } = 15;

    /// <summary>
    /// Gets or sets the relative improvement required to switch parent.
    /// </summary>
    public double ParentSwitchHysteresisRatio { get; set; } = 0.03;

    /// <summary>
    /// Gets the local UP charge estimate.
    /// </summary>
    public ushort QUpSelf { get; private set; }

    /// <summary>
    /// Gets the local TOTAL charge estimate.
    /// </summary>
    public ushort QTotalSelf { get; private set; }

    /// <summary>
    /// Gets the latest decay epoch applied on this device.
    /// </summary>
    public ushort LastDecayEpoch { get; private set; }

    /// <summary>
    /// Gets the currently selected parent MAC for DOWN tree forwarding.
    /// </summary>
    public byte[]? ParentMac { get; private set; }

    /// <summary>
    /// Gets the tick when the selected parent was last observed.
    /// </summary>
    public long ParentLastSeenTick { get; private set; }

    /// <summary>
    /// Gets a read-only view of known neighbors.
    /// </summary>
    public IReadOnlyCollection<NeighborState> Neighbors => _neighbors.Values;

    /// <summary>
    /// Gets a value indicating whether this device is currently eligible to
    /// forward DOWN traffic.
    /// </summary>
    public bool IsForwardEligible => QTotalSelf >= QForwardThreshold;

    /// <summary>
    /// Called when a packet arrives at this device.
    /// If the packet is addressed to another device, routing is delegated to
    /// <see cref="SimulationEngine.RoutePacket"/> which uses the currently
    /// configured <see cref="Engine.Routers.IPacketRouter"/>.
    /// If the packet is addressed to this device and delivery confirmation was
    /// requested, a <see cref="ConfirmationPacket"/> is routed back to the
    /// originator before <see cref="Accept"/> is invoked.
    /// </summary>
    /// <param name="packet">The packet that has arrived at this device.</param>
    public void Recieve(Packet packet)
    {
        var currentTick = SimulationEngine.Instance.TickCount;

        if (packet.PreviousHop is not null)
            ObserveNeighbor(packet.PreviousHop, packet, currentTick);

        if (!TryRegisterDedup(packet))
            return;

        ApplyControlPacketEffects(packet);
        EnsureParentStillAlive(currentTick);

        var addressedToThis =
            packet.To.Equals(this) ||
            PacketAddress.EqualsMac(packet.DestinationMac, MacAddress);

        if (!addressedToThis)
        {
            OnForwardPacket(packet);

            packet.PreviousHop   = this;
            packet.PreviousHopMac = PacketAddress.Clone(MacAddress);
            packet.AdvertisedCharge = packet.Direction == PacketDirection.Up
                ? QUpSelf
                : QTotalSelf;
            packet.DecayEpochHint = LastDecayEpoch;

            SimulationEngine.Instance.RoutePacket(packet, this);
            return;
        }

        if (packet.NeedConfirmation)
        {
            var confirmationPacket = new ConfirmationPacket(packet);
            SimulationEngine.Instance.RoutePacket(confirmationPacket, this);
        }
        Accept(packet);
    }

    /// <summary>
    /// Returns the best known neighbor charge for a given address and
    /// direction.
    /// </summary>
    /// <param name="macAddress">Neighbor MAC address.</param>
    /// <param name="direction">Direction to query.</param>
    /// <returns>Known charge value or 0 when the neighbor is unknown.</returns>
    public ushort GetNeighborCharge(ReadOnlySpan<byte> macAddress, PacketDirection direction)
    {
        if (!_neighbors.TryGetValue(PacketAddress.ToKey(macAddress), out var state))
            return 0;

        return direction == PacketDirection.Up ? state.QUp : state.QTotal;
    }

    /// <summary>
    /// Returns <c>true</c> when this device currently uses <paramref name="device"/>
    /// as its selected DOWN-tree parent.
    /// </summary>
    /// <param name="device">Candidate parent device.</param>
    /// <returns><c>true</c> when parent MAC matches the candidate.</returns>
    public bool HasParent(Device device)
        => ParentMac is not null && PacketAddress.EqualsMac(ParentMac, device.MacAddress);

    /// <summary>
    /// Applies a swarm parameter vector to this device.
    /// </summary>
    /// <param name="vector">Swarm protocol parameter vector.</param>
    public virtual void ApplySwarmVector(SwarmProtocolVector vector)
    {
        var normalized = vector.Normalized();

        QForwardThreshold = (ushort)Math.Clamp(normalized.QForward, 0, ushort.MaxValue);
        ParentSwitchHysteresis = (ushort)Math.Clamp(normalized.SwitchHysteresis, 0, ushort.MaxValue);
        ParentSwitchHysteresisRatio = normalized.SwitchHysteresisRatio;
        ParentDeadTicks = Math.Max(1, normalized.ParentDeadTicks);
    }

    /// <summary>
    /// Returns a link-quality bonus score for the given neighbor.
    /// </summary>
    /// <param name="macAddress">Neighbor MAC address.</param>
    /// <param name="maxBonus">Maximum possible bonus.</param>
    /// <returns>Integral bonus in range [0, maxBonus].</returns>
    public int GetNeighborLinkBonus(ReadOnlySpan<byte> macAddress, int maxBonus)
    {
        if (maxBonus <= 0)
            return 0;

        if (!_neighbors.TryGetValue(PacketAddress.ToKey(macAddress), out var state))
            return 0;

        var quality = Math.Clamp(state.LinkQuality, 0.0, 1.0);
        return (int)Math.Round(maxBonus * quality, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Applies charge decay for a new epoch and re-evaluates the parent.
    /// </summary>
    /// <param name="decayEpoch">Monotonic decay epoch.</param>
    /// <param name="decayPercent">Decay factor in range [0, 1].</param>
    public void ApplyDecay(ushort decayEpoch, double decayPercent)
    {
        if (decayEpoch <= LastDecayEpoch)
            return;

        var clampedPercent = Math.Clamp(decayPercent, 0.0, 1.0);
        var epochs = decayEpoch - LastDecayEpoch;
        var damping = Math.Pow(1.0 - clampedPercent, epochs);

        QUpSelf = ScaleCharge(QUpSelf, damping);
        QTotalSelf = ScaleCharge(QTotalSelf, damping);

        foreach (var state in _neighbors.Values)
        {
            state.QUp = ScaleCharge(state.QUp, damping);
            state.QTotal = ScaleCharge(state.QTotal, damping);
        }

        LastDecayEpoch = decayEpoch;
        ReevaluateParent(SimulationEngine.Instance.TickCount);
    }

    /// <summary>
    /// Overwrites local charge levels. Intended for bootstrap devices such as
    /// the hub gateway.
    /// </summary>
    /// <param name="qUp">Initial UP charge.</param>
    /// <param name="qTotal">Initial total charge.</param>
    protected void SetChargeLevels(ushort qUp, ushort qTotal)
    {
        QUpSelf = qUp;
        QTotalSelf = qTotal;
    }

    /// <summary>
    /// Invoked before this device forwards a packet.
    /// </summary>
    /// <param name="packet">Packet being forwarded.</param>
    protected virtual void OnForwardPacket(Packet packet)
    {
        if (packet.Direction == PacketDirection.Up)
        {
            QUpSelf = SaturatingIncrement(QUpSelf);
            QTotalSelf = SaturatingIncrement(QTotalSelf);
            return;
        }

        QTotalSelf = SaturatingIncrement(QTotalSelf);
    }

    internal ushort AllocateMessageId()
    {
        var value = _nextMessageId;
        _nextMessageId = _nextMessageId == ushort.MaxValue
            ? (ushort)1
            : (ushort)(_nextMessageId + 1);
        return value;
    }

    internal ushort AllocateSequence()
    {
        var value = _nextSequence;
        _nextSequence = _nextSequence == ushort.MaxValue
            ? (ushort)1
            : (ushort)(_nextSequence + 1);
        return value;
    }

    /// <summary>
    /// Processes a packet that is addressed to and has been delivered to this device.
    /// Derived classes implement the specific handling logic here.
    /// </summary>
    /// <param name="packet">The delivered packet to process.</param>
    public abstract void Accept(Packet packet);

    private void ObserveNeighbor(Device neighbor, Packet packet, long currentTick)
    {
        var vector = SimulationEngine.Instance.SwarmVector;
        var key = PacketAddress.ToKey(neighbor.MacAddress);
        if (!_neighbors.TryGetValue(key, out var state))
        {
            state = new NeighborState(neighbor.MacAddress);
            _neighbors.Add(key, state);
        }

        state.LastSeenTick = currentTick;
        state.SampleCount++;

        if (packet.Direction == PacketDirection.Up)
        {
            state.QUp = UpdateChargeEstimate(state.QUp, packet.AdvertisedCharge, vector.ChargeSpreadFactor);
        }
        else
        {
            state.QTotal = UpdateChargeEstimate(state.QTotal, packet.AdvertisedCharge, vector.ChargeSpreadFactor);
        }

        UpdateLinkQuality(state, vector);

        ReevaluateParent(currentTick);
    }

    private bool TryRegisterDedup(Packet packet)
    {
        var key = string.Create(
            4 + PacketAddress.AddressLength * 2 + 1,
            packet,
            static (span, p) =>
            {
                PacketAddress.ToKey(p.OriginMac).AsSpan().CopyTo(span);
                var separatorIndex = PacketAddress.AddressLength * 2;
                span[separatorIndex] = ':';
                p.MessageId
                    .ToString("X4", CultureInfo.InvariantCulture)
                    .AsSpan()
                    .CopyTo(span[(separatorIndex + 1)..]);
            });

        if (_dedupCache.Contains(key))
            return false;

        _dedupCache.Add(key);
        _dedupOrder.Enqueue(key);

        while (_dedupOrder.Count > DedupCacheCapacity)
        {
            var oldest = _dedupOrder.Dequeue();
            _dedupCache.Remove(oldest);
        }

        return true;
    }

    private void ApplyControlPacketEffects(Packet packet)
    {
        if (packet.MessageType == SwarmMessageType.BEACON &&
            packet.Payload.Data is BeaconPayload beacon &&
            beacon.RecommendedForwardThreshold > 0)
        {
            QForwardThreshold = beacon.RecommendedForwardThreshold;
            ReevaluateParent(SimulationEngine.Instance.TickCount);
            return;
        }

        if (packet.MessageType == SwarmMessageType.DECAY &&
            packet.Payload.Data is DecayPayload decay)
        {
            ApplyDecay(decay.DecayEpoch, decay.DecayPercent);
        }
    }

    private void EnsureParentStillAlive(long currentTick)
    {
        if (ParentMac is null || ParentDeadTicks <= 0)
            return;

        if (currentTick - ParentLastSeenTick <= ParentDeadTicks)
            return;

        ParentMac = null;
        ParentLastSeenTick = 0;
    }

    private void ReevaluateParent(long currentTick)
    {
        RemoveStaleNeighbors(currentTick);

        NeighborState? currentParent = null;
        if (ParentMac is not null)
            _neighbors.TryGetValue(PacketAddress.ToKey(ParentMac), out currentParent);

        NeighborState? best = null;
        foreach (var state in _neighbors.Values)
        {
            if (state.QTotal < QForwardThreshold)
                continue;

            if (state.QTotal <= QTotalSelf)
                continue;

            if (best is null)
            {
                best = state;
                continue;
            }

            if (state.QTotal > best.QTotal)
            {
                best = state;
                continue;
            }

            if (state.QTotal == best.QTotal &&
                PacketAddress.Compare(state.MacAddress, best.MacAddress) < 0)
            {
                best = state;
            }
        }

        if (best is null)
        {
            ParentMac = null;
            ParentLastSeenTick = 0;
            return;
        }

        if (currentParent is not null &&
            !PacketAddress.EqualsMac(currentParent.MacAddress, best.MacAddress))
        {
            var improvement = (int)best.QTotal - currentParent.QTotal;
            var ratio = currentParent.QTotal == 0
                ? 1.0
                : (double)improvement / currentParent.QTotal;

            if (improvement < ParentSwitchHysteresis ||
                ratio < ParentSwitchHysteresisRatio)
            {
                ParentMac = PacketAddress.Clone(currentParent.MacAddress);
                ParentLastSeenTick = currentParent.LastSeenTick;
                return;
            }
        }

        ParentMac = PacketAddress.Clone(best.MacAddress);
        ParentLastSeenTick = best.LastSeenTick;
    }

    private void RemoveStaleNeighbors(long currentTick)
    {
        if (ParentDeadTicks <= 0)
            return;

        var staleTick = currentTick - ParentDeadTicks;
        var staleKeys = _neighbors
            .Where(x => x.Value.LastSeenTick < staleTick)
            .Select(x => x.Key)
            .ToArray();

        foreach (var key in staleKeys)
            _neighbors.Remove(key);
    }

    private static ushort SaturatingIncrement(ushort value)
        => value == ushort.MaxValue ? ushort.MaxValue : (ushort)(value + 1);

    private static ushort UpdateChargeEstimate(ushort current, ushort advertised, double spreadFactor)
    {
        if (advertised <= current)
            return current;

        var factor = Math.Clamp(spreadFactor, 0.01, 1.0);
        var blended = current + (advertised - current) * factor;
        return (ushort)Math.Clamp(
            (int)Math.Round(blended, MidpointRounding.AwayFromZero),
            0,
            ushort.MaxValue);
    }

    private static void UpdateLinkQuality(NeighborState state, SwarmProtocolVector vector)
    {
        var memory = Math.Clamp(vector.LinkMemory, 0.6, 0.999);
        var learning = Math.Clamp(vector.LinkLearningRate, 0.01, 2.0);

        var sampleWeight = (1.0 - memory) * learning;
        var next = state.LinkQuality * memory + sampleWeight;
        state.LinkQuality = Math.Clamp(next, 0.0, 1.0);
    }

    private static ushort ScaleCharge(ushort value, double factor)
    {
        var scaled = (int)Math.Round(value * factor, MidpointRounding.AwayFromZero);
        return (ushort)Math.Clamp(scaled, 0, ushort.MaxValue);
    }
}