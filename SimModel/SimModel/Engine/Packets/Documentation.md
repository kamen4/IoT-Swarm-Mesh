# Engine/Packets

The `Packets` folder contains all packet types that travel through the simulation network, along with the untyped payload wrapper.

---

## Files

| File                    | Responsibility                                                                                                                                                                                                                                  |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Packet.cs`             | Core transmission unit. Carries `From`, `To`, `NextHop`, `ArrivalTick`, `TTL`, `OriginId`, `InitialTtl`, and a `PacketData` payload. `Clone()` creates a shallow copy preserving `OriginId` for flood-clone tracking by `SimulationStatistics`. |
| `PacketData.cs`         | Untyped payload wrapper. The `Data` property holds any application-level value (sensor reading, command flag, raw bytes). The receiving device is responsible for casting to the expected type.                                                 |
| `ConfirmationPacket.cs` | Acknowledgement packet routed back to the original sender when `Packet.NeedConfirmation` is true. Swaps `From` and `To` of the original packet and carries the original `Id` encoded as bytes.                                                  |
| `ControlPacket.cs`      | Command packet sent by `HubDevice` to a specific `EmitterDevice`. Carries a `bool Command` (true = on, false = off) that the emitter applies to its `State` on receipt.                                                                         |

---

## Key concepts

- **TTL**  -  decremented on every hop; packet is dropped and `PacketExpired` is raised when TTL reaches zero.
- **OriginId**  -  equals `Id` on the original; preserved through `Clone()` so statistics can detect duplicate flood-clone deliveries.
- **InitialTtl**  -  stamped once by `SimulationEngine.RegisterPacket` on first enqueue; enables hop-count calculation as `InitialTtl - TTL` at delivery.

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
