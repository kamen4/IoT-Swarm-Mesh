# UartLS

## Purpose and boundary

UartLS (UART Listener/Sender) bridges the Redis message bus and the physical serial port that
connects to GatewayDevice. It subscribes to hub:cmd, translates commands into the UART wire
protocol, reads the device echo, and publishes the result to hub:evt.
Boundary: UartLS is the only Hub component that opens a SerialPort; all other concerns (HTTP,
Telegram) are outside its scope.

## Subfolders

| Folder      | Purpose                                   |
| ----------- | ----------------------------------------- |
| Workers/    | BackgroundService implementations         |
| Properties/ | launchSettings.json for local run         |

## Files

### Root

| File                         | Responsibility                                                            |
| ---------------------------- | ------------------------------------------------------------------------- |
| Program.cs                   | Worker Service host: registers IConnectionMultiplexer, UartBridgeWorker   |
| UartLS.csproj                | .NET project; references Common; NuGet: StackExchange.Redis, System.IO.Ports|
| Dockerfile                   | Multi-stage build; requires devices: mount in docker-compose              |
| appsettings.json             | Default config: Redis connection string and serial port settings          |
| appsettings.Development.json | Overrides for local development                                           |

### Workers/

| File                | Responsibility                                                            |
| ------------------- | ------------------------------------------------------------------------- |
| UartBridgeWorker.cs | Core bridge logic:                                                        |
|                     | 1. Subscribes to Redis hub:cmd                                            |
|                     | 2. Deserialises PinCommandMessage                                         |
|                     | 3. Writes "PIN_TOGGLE:{pin}\n" to SerialPort                              |
|                     | 4. Reads "PIN_STATE:{pin}:{0|1}\n" from SerialPort (2 s timeout)          |
|                     | 5. Publishes PinEventMessage to Redis hub:evt                             |

## UART wire protocol

| Direction     | Line format              | Example         |
| ------------- | ------------------------ | --------------- |
| Hub -> Device | PIN_TOGGLE:{pin}\n       | PIN_TOGGLE:8\n  |
| Device -> Hub | PIN_STATE:{pin}:{state}\n| PIN_STATE:8:1\n |

Line terminator: LF (0x0A). Serial settings: 115200 baud, 8N1.

## Configuration

| Key                    | Default       | Description                         |
| ---------------------- | ------------- | ----------------------------------- |
| Redis:ConnectionString | redis:6379    | Redis server address                |
| SerialPort:PortName    | /dev/ttyUSB0  | Path to the serial device           |
| SerialPort:BaudRate    | 115200        | Serial baud rate                    |

Overridden in docker-compose.yml via environment variables UART_PORT and UART_BAUD from .env.

## Docker serial port access

UartLS must have the host serial device mounted into the container. In docker-compose.yml:

```yaml
devices:
  - ${UART_PORT:-/dev/ttyUSB0}:${UART_PORT:-/dev/ttyUSB0}
```

Without this the container cannot open the SerialPort. Update UART_PORT in .env when the port
changes.

## Interactions and constraints

- UartBridgeWorker is the sole reader/writer of the SerialPort; only one instance may run.
- CorrelationId from PinCommandMessage must be forwarded unchanged into PinEventMessage so that
  BusinessServer can match the response to the pending request.
- If the device does not reply, the 2 s read timeout unblocks the handler.
  OPEN DECISION: error reporting strategy on timeout (currently: no PinEventMessage published).
- On container restart the Redis subscription is re-established automatically.
- UartLS exposes no HTTP endpoints; it communicates only through Redis.

## Extension points

- New command type: add a new branch in UartBridgeWorker for the new message type; update the
  UART protocol section above.

## Relation to parent folder

UartLS is the transport adapter in Hub.
It depends on Redis (message bus) and the host serial device.
Deployed as a container via docker-compose.yml with a devices: mount entry.
