# Engine/Packets

The `Packets` folder contains all packet types that travel through the simulation network, including the swarm protocol envelope model and helper payloads.

---

## Files

| File | Responsibility |
| --- | --- |
| `Packet.cs` | Core transmission unit. Carries transport fields (`From`, `To`, `NextHop`, `ArrivalTick`, `TTL`) and protocol envelope fields (`Direction`, `MessageType`, `OriginMac`, `DestinationMac`, `MessageId`, `Sequence`, `AdvertisedCharge`, `DecayEpochHint`, `Tag`). Provides `BuildSecureHeaderAndPayload()`, `UpdateTag()`, and `ValidateTag()`. |
| `PacketData.cs` | Untyped payload wrapper. The `Data` property holds any application-level value (sensor reading, command flag, raw bytes). The receiving device is responsible for casting to the expected type. |
| `ConfirmationPacket.cs` | Acknowledgement packet routed back to the original sender when `Packet.NeedConfirmation` is true. Swaps `From` and `To` of the original packet and carries the original `Id` encoded as bytes. |
| `ControlPacket.cs` | Command packet sent by `HubDevice` to a specific `EmitterDevice`. Carries a `bool Command` (true = on, false = off) that the emitter applies to its `State` on receipt. |
| `BeaconPacket.cs` | Gateway broadcast packet (`BEACON`) carrying a `BeaconPayload` with a recommended `q_forward` threshold for convergence tuning. |
| `DecayPacket.cs` | Gateway broadcast packet (`DECAY`) carrying a `DecayPayload` with monotonic decay epoch and damping factor. |
| `PacketDirection.cs` | `PacketDirection` enum (`Up`, `Down`) used by the secure header model. |
| `SwarmMessageType.cs` | Protocol message-type registry (FIND/PONG/VERIFY/PROTO/ACK/HELLO/BEACON/DECAY/FRAG and IO message family). |
| `PacketAddress.cs` | Fixed-size 6-byte address helpers: GUID-to-MAC derivation, broadcast constant, compare/equality, and stable key conversion. |
| `ProtocolPayloads.cs` | Record payloads for protocol control traffic (`BeaconPayload`, `DecayPayload`). |

---

## Key concepts

- **Protocol envelope mapping** - `Packet` models both protocol headers.
- Routing header fields: `Version`, `PreviousHopMac`, `AdvertisedCharge`, `DecayEpochHint`.
- Secure header fields: `Direction`, `MessageType`, `OriginMac`, `DestinationMac`, `MessageId`, `Sequence`.
- **Tag computation rule** - `UpdateTag()` and `ValidateTag()` use `HMAC-SHA256` over `SECURE_HEADER | PAYLOAD` only, truncated to 16 bytes. Routing fields are intentionally excluded because hops mutate them.
- **TTL** - decremented on every hop; packet is dropped and `PacketExpired` is raised when TTL reaches zero.
- **OriginId** - equals `Id` on the original; preserved through `Clone()` so statistics can detect duplicate flood-clone deliveries.
- **InitialTtl** - stamped once by `SimulationEngine.RegisterPacket` on first enqueue; enables hop-count calculation as `InitialTtl - TTL` at delivery.

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
