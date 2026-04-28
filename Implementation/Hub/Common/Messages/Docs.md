# Messages

## Purpose and Boundary

Redis pub/sub message payload types for the hub:cmd and hub:evt channels.
These types define the binary/JSON contract between BusinessServer (publisher of commands) and UartLS (subscriber of commands, publisher of events).

## Files

| File                 | Type              | Channel | Fields                                                      |
| -------------------- | ----------------- | ------- | ----------------------------------------------------------- |
| PinCommandMessage.cs | PinCommandMessage | hub:cmd | CorrelationId (GUID string), Pin (int)                      |
| PinEventMessage.cs   | PinEventMessage   | hub:evt | CorrelationId (GUID string), Pin (int), State (bool or int) |

## Interactions and Constraints

- These types must remain byte-for-byte identical on both ends (BusinessServer and UartLS); any field rename or type change breaks the pub/sub contract.
- CorrelationId is a GUID string; it links each PinCommandMessage published on hub:cmd to the matching PinEventMessage on hub:evt.
- BusinessServer serializes PinCommandMessage -> publishes to hub:cmd -> UartLS deserializes.
- UartLS serializes PinEventMessage -> publishes to hub:evt -> BusinessServer/PinEventListener deserializes.
- No logic; data containers only.
- These types are NOT HTTP types and must not appear in controller signatures or DTO files.

## Relation to Parent Folder

Sits inside Common, which is referenced by both BusinessServer and UartLS.
If TelegramServer ever needs event data, it must obtain it through the BusinessServer HTTP API, not by subscribing to Redis directly.
