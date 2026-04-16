# Engine/Devices

The `Devices` folder contains the abstract base class and all concrete device types that participate in the simulation.

---

## Files

| File | Responsibility |
| --- | --- |
| `Device.cs` | Abstract base for all devices. Holds identity (`Id`, `MacAddress`), 2D position (`Vector2`), dedup cache, neighbor charge table, local charges (`QUpSelf`, `QTotalSelf`), and DOWN-tree parent state. `Recieve(Packet)` applies dedup, updates neighbor observations (`q_up` from UP traffic and `q_total` from DOWN traffic), applies protocol control effects (BEACON/DECAY), and delegates forwarding to `SimulationEngine.RoutePacket`. |
| `NeighborState.cs` | Protocol neighbor model: `MacAddress`, `QUp`, `QTotal`, `LastSeenTick`, `SampleCount`. Used for best-neighbor routing decisions and parent selection hysteresis. |
| `HubDevice.cs` | Central gateway device and protocol root. Emits periodic `BeaconPacket` and `DecayPacket` on tick events, applies local decay epochs, and starts with high baseline charge to seed convergence. |
| `GeneratorDevice.cs` | Sensor-like device that self-emits one data packet toward the `Hub` every `GenFrequencyTicks` ticks. Subscribes to `SimulationEngine.TickEvent` on construction. |
| `EmitterDevice.cs` | Actuator-like device that accepts `ControlPacket` commands from the `Hub`. Maintains a boolean `State` (on/off). The Hub drives it by sending `ControlPacket` at `ControlFrequencyTicks` intervals. |

---

## Inheritance

```text
Device (abstract)
+-- HubDevice
+-- GeneratorDevice
+-- EmitterDevice
```

---

## Extension points

Add new device types by subclassing `Device` and optionally subscribing to `SimulationEngine.TickEvent` for periodic behavior.

Protocol-relevant extension points in `Device`:

- `OnForwardPacket(Packet)` for custom charge accumulation policy.
- `ApplyDecay(epoch, percent)` for alternative damping behavior.
- Parent-selection tuning via `QForwardThreshold`, `ParentSwitchHysteresis`, `ParentSwitchHysteresisRatio`, and `ParentDeadTicks`.

---

## Parent

See `Engine/Documentation.md` for full engine architecture.
