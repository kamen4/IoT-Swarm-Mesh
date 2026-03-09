# Engine/Statistics

The `Statistics` folder contains the reactive simulation metrics subsystem. It listens to `SimulationEngine` events and accumulates observable metric values and a per-tick history ring buffer.

---

## Files

| File                      | Responsibility                                                                                                                                                                                                                          |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SimulationStatistics.cs` | Singleton that subscribes to all `SimulationEngine` events and maintains ten `StatMetric` values. Appends a `TickSnapshot` to the history ring buffer on every tick. Raises `Updated` after each change so UI can re-render reactively. |
| `StatMetric.cs`           | A single named metric with a `double Value`, display format flags (`isDecimal`, `isPlottable`), and an `Updated` event. `Increment()` and `Set(value)` update the value and notify subscribers.                                         |
| `TickSnapshot.cs`         | Immutable record capturing a point-in-time snapshot of the eight plottable metrics for one tick. Used for time-series chart rendering.                                                                                                  |

---

## Metrics tracked

| Metric               | Description                                                                |
| -------------------- | -------------------------------------------------------------------------- |
| Packets registered   | All enqueued packets including flood clones.                               |
| Packets delivered    | Packets that reached their destination.                                    |
| Packets expired      | Packets dropped because TTL hit zero.                                      |
| Devices added        | Cumulative device registrations since last reset.                          |
| Total ticks          | Engine ticks elapsed since last reset.                                     |
| Avg tick (ms)        | Running mean of wall-clock time per tick.                                  |
| Active packets       | Packets currently in-flight.                                               |
| Delivery rate (%)    | `Delivered / (Delivered + Expired) x 100`.                                 |
| Duplicate deliveries | Flood clones that arrived after first delivery of the same logical packet. |
| Avg hop count        | Mean `InitialTtl - TTL` at first delivery.                                 |

---

## Extension points

Add a new `StatMetric` field to `SimulationStatistics`, subscribe to the relevant engine event, and set `isPlottable: true` to expose it as a chart series.

---

## Parent

See `Engine/Documentation.md` for the full engine architecture.
