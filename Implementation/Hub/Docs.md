# Hub

## Purpose and boundary

Hub is the server-side of the IoT-Swarm-Mesh system.
It receives user commands via Telegram, routes them to the physical gateway device over a serial
port, and delivers the echo response back to the user.
Boundary: everything running in containers belongs to Hub; firmware flashed to microcontrollers
belongs to Implementation/Devices.

## Subfolders

| Folder          | Purpose                                                                 |
| --------------- | ----------------------------------------------------------------------- |
| BusinessServer/ | HTTP API; orchestrates Redis pub/sub between TelegramServer and UartLS  |
| TelegramServer/ | Telegram bot front-end; the only user-facing component                  |
| UartLS/         | UART Listener/Sender; bridge between Redis and the physical serial port |
| Common/         | Shared .NET library: HTTP DTOs and Redis message contracts (no logic)   |
| grafana/        | Grafana provisioning configuration (dashboards, datasources)            |

## Files

| File               | Responsibility                                               |
| ------------------ | ------------------------------------------------------------ |
| docker-compose.yml | Orchestrates all containers and infrastructure               |
| IoTSwarmHub.slnx   | .NET solution file for all Hub projects                      |
| .env.example       | Environment variable template; copy to .env before first run |
| .gitignore         | Excludes .env, bin/, obj/, and build artifacts from VCS      |

## Infrastructure (docker-compose.yml)

| Service         | Image                       | Port | Role                                       |
| --------------- | --------------------------- | ---- | ------------------------------------------ |
| redis           | redis:7-alpine              | 6379 | Message bus: channels hub:cmd / hub:evt    |
| postgres        | postgres:16-alpine          | 5432 | Relational store (reserved, schema TBD)    |
| influxdb        | influxdb:2-alpine           | 8086 | Time-series telemetry storage              |
| grafana         | grafana/grafana:latest      | 3000 | Metrics visualisation                      |
| business-server | ./BusinessServer/Dockerfile | 8080 | HTTP API + Redis publisher                 |
| telegram-server | ./TelegramServer/Dockerfile | --   | Telegram long polling                      |
| uart-ls         | ./UartLS/Dockerfile         | --   | UART bridge; mounts /dev:/dev (privileged) |

All services share the bridge network hub-net.
uart-ls mounts the full host /dev volume (/dev:/dev) with privileged: true. The device is an
ESP32-C3 USB CDC-ACM adapter that appears as /dev/ttyACM* on Linux. A stable udev symlink
/dev/ttyGATEWAY is created via /etc/udev/rules.d/99-iot-gateway.rules. Set UART_PORT=/dev/ttyGATEWAY
in .env so the port path is stable regardless of USB enumeration order.

## End-to-end trace

```
User -> Telegram
  -> TelegramServer (long poll receives update)
    -> POST /api/pin/toggle -> BusinessServer (PinController)
      -> Redis PUBLISH hub:cmd  PinCommandMessage{CorrelationId, Pin=8}
        -> UartLS (subscribed to hub:cmd)
          -> SerialPort WRITE  "PIN_TOGGLE:8\n"
            -> GatewayDevice (ESP32-C3): toggles GPIO 8
          <- SerialPort READ   "PIN_STATE:8:1\n"
        -> Redis PUBLISH hub:evt  PinEventMessage{CorrelationId, Pin=8, State=1}
      <- BusinessServer PinEventListener resolves TaskCompletionSource
    <- HTTP 200  {pin:8, state:1}
  <- TelegramServer sends "Gateway pin 8 is now HIGH"
<- User receives message
```

Timeout for awaiting the device echo in BusinessServer: 5 seconds.

## Interactions and constraints

- UartLS is the only component that directly accesses the serial port.
- Redis is the sole bus between BusinessServer and UartLS; no direct HTTP calls between them.
- TelegramServer does not connect to Redis or UartLS.
- BusinessServer is the single HTTP entry point for commands from TelegramServer.
- CorrelationId links command and response across Redis channels (hub:cmd -> hub:evt).

## Extension points

- New command type: add a message type in Common/Messages, a handler in UartLS, an endpoint in
  BusinessServer, and a case in TelegramServer/BotUpdateHandler.
- Monitoring: InfluxDB and Grafana are already wired; write metrics in BusinessServer or UartLS.
- Event history: PostgreSQL is connected; schema and persistence are OPEN DECISION.

## Relation to parent folder

Hub is one of the two main parts of Implementation (the other is Devices).
Hub implements the server side of the protocol defined in Protocol/_docs_v1.0.
The Hub <-> GatewayDevice wire contract is defined by the UART protocol (PIN_TOGGLE / PIN_STATE).
