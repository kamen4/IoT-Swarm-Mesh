# Workers

## Purpose and Boundary

BackgroundService implementations for UartLS.
This folder contains the long-running worker that bridges the Redis pub/sub bus and the physical serial port connected to the ESP32 gateway device.

## Files

| File                | Role                                                                                                                                                  |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| UartBridgeWorker.cs | Subscribes to Redis hub:cmd; opens the serial port with retry; writes PIN_TOGGLE command; reads PIN_STATE response; publishes result to Redis hub:evt |

## Interactions and Constraints

- Only one instance of UartBridgeWorker may open the serial port at a time.
- `_port` field is volatile and remains null until the port is successfully opened.
- DTR (Data Terminal Ready) must be cleared after Open() to prevent the ESP32 from entering reset via the DTR line.
- Serial port open is retried with a backoff loop; failures are logged and retried, not surfaced as exceptions.
- Worker reads a single-line response after each write; no streaming read.
- Worker must not reference HTTP controllers or Telegram SDK types.
- All message payloads are deserialized from Common/Messages types (PinCommandMessage, PinEventMessage).

## Relation to Parent Folder

UartBridgeWorker is registered in Program.cs using AddHostedService<UartBridgeWorker>().
Program.cs also configures IConnectionMultiplexer (Redis) and serial port settings via IConfiguration.
No other classes in UartLS should access the serial port directly.
