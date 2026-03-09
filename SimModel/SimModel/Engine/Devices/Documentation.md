# Engine/Devices

The `Devices` folder contains the abstract base class and all concrete device types that participate in the simulation.

---

## Files

| File                 | Responsibility                                                                                                                                                                                                                                                               |
| -------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Device.cs`          | Abstract base for all devices. Holds a unique `Id` (GUID), a mutable `Name`, and a 2D `Position` (`Vector2`). Defines `Recieve(Packet)` which stamps `PreviousHop`, sends a `ConfirmationPacket` if requested, and delegates re-routing to `SimulationEngine.RoutePacket`.   |
| `HubDevice.cs`       | Central gateway device; the intended destination for all packets from `GeneratorDevice`. Overrides `Recieve` to also send queued `ControlPacket` commands to `EmitterDevice` instances on each tick at `ControlFrequencyTicks` (delegated via `SimulationEngine.TickEvent`). |
| `GeneratorDevice.cs` | Sensor-like device that self-emits one data packet toward the `Hub` every `GenFrequencyTicks` ticks. Subscribes to `SimulationEngine.TickEvent` on construction.                                                                                                             |
| `EmitterDevice.cs`   | Actuator-like device that accepts `ControlPacket` commands from the `Hub`. Maintains a boolean `State` (on/off). The Hub drives it by sending `ControlPacket` at `ControlFrequencyTicks` intervals.                                                                          |

---

## Inheritance

```
Device (abstract)
+-- HubDevice
+-- GeneratorDevice
+-- EmitterDevice
```

---

## Extension points

Add new device types by subclassing `Device` and optionally subscribing to `SimulationEngine.TickEvent` for periodic behavior.

---

## Parent

See `Engine/Documentation.md` for full engine architecture.
