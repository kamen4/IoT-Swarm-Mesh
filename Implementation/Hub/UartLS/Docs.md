# UartLS

## Purpose and boundary

UartLS (UART Listener/Sender) bridges the Redis message bus and the physical serial port that
connects to GatewayDevice. It subscribes to hub:cmd, translates commands into the UART wire
protocol, reads the device echo, and publishes the result to hub:evt.
Boundary: UartLS is the only Hub component that opens a SerialPort; all other concerns (HTTP,
Telegram) are outside its scope.

## Subfolders

| Folder      | Purpose                           |
| ----------- | --------------------------------- |
| Workers/    | BackgroundService implementations |
| Properties/ | launchSettings.json for local run |

## Files

### Root

| File                         | Responsibility                                                               |
| ---------------------------- | ---------------------------------------------------------------------------- |
| Program.cs                   | Worker Service host: registers IConnectionMultiplexer, UartBridgeWorker      |
| UartLS.csproj                | .NET project; references Common; NuGet: StackExchange.Redis, System.IO.Ports |
| Dockerfile                   | Multi-stage build; requires /dev:/dev volume + privileged in docker-compose  |
| appsettings.json             | Default config: Redis connection string and serial port settings             |
| appsettings.Development.json | Overrides for local development                                              |

### Workers/

| File                | Responsibility                                |
| ------------------- | --------------------------------------------- |
| UartBridgeWorker.cs | Core bridge logic:                            |
|                     | 1. Subscribes to Redis hub:cmd                |
|                     | 2. Deserialises PinCommandMessage             |
|                     | 3. Writes "PIN_TOGGLE:{pin}\n" to SerialPort  |
|                     | 4. Reads "PIN_STATE:{pin}:{0                  | 1}\n" from SerialPort (2 s timeout) |
|                     | 5. Publishes PinEventMessage to Redis hub:evt |

## UART wire protocol

| Direction     | Line format               | Example         |
| ------------- | ------------------------- | --------------- |
| Hub -> Device | PIN_TOGGLE:{pin}\n        | PIN_TOGGLE:8\n  |
| Device -> Hub | PIN_STATE:{pin}:{state}\n | PIN_STATE:8:1\n |

Line terminator: LF (0x0A). Serial settings: 115200 baud, 8N1.

## Configuration

| Key                    | Default      | Description               |
| ---------------------- | ------------ | ------------------------- |
| Redis:ConnectionString | redis:6379   | Redis server address      |
| SerialPort:PortName    | /dev/ttyACM0 | Path to the serial device |
| SerialPort:BaudRate    | 115200       | Serial baud rate          |

Overridden in docker-compose.yml via environment variables UART_PORT and UART_BAUD from .env.

## Docker serial port access

UartLS requires access to the host serial device inside the container. The full host /dev tree is
mounted and the service runs with elevated privileges so that all device nodes, including udev
symlinks, are accessible:

```yaml
volumes:
  - /dev:/dev
privileged: true
```

Without this the container cannot open the SerialPort.

The GatewayDevice is an ESP32-C3 USB CDC-ACM adapter that appears as /dev/ttyACM* on Linux.
A stable udev symlink /dev/ttyGATEWAY is created via /etc/udev/rules.d/99-iot-gateway.rules
so that UART_PORT can always be set to /dev/ttyGATEWAY regardless of USB enumeration order.

> Note: Docker `devices:` entries do not resolve host symlinks; the `/dev:/dev` volume mount is
> required to expose /dev/ttyGATEWAY inside the container.

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
Deployed as a container via docker-compose.yml with /dev:/dev volume mount and privileged: true.
