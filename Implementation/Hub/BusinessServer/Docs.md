# BusinessServer

## Purpose and boundary

BusinessServer is the HTTP API gateway between TelegramServer and the device layer (UartLS /
GatewayDevice). It receives REST commands from TelegramServer, publishes them to Redis as
PinCommandMessage, and awaits confirmation through PinEventListener.
Boundary: all business logic and Redis orchestration lives here; the UART transport layer is
in UartLS.

## Subfolders

| Folder       | Purpose                                                     |
| ------------ | ----------------------------------------------------------- |
| Controllers/ | ASP.NET Core controllers; define HTTP routes                |
| Services/    | Service layer: command dispatch, Redis pub/sub, device reg. |
| Properties/  | launchSettings.json for local run                           |

## Files

### Root

| File                         | Responsibility                                                     |
| ---------------------------- | ------------------------------------------------------------------ |
| Program.cs                   | Entry point: DI registration, Kestrel config, route mapping        |
| BusinessServer.csproj        | .NET project; references Common                                    |
| Dockerfile                   | Multi-stage build; base image mcr.microsoft.com/dotnet/aspnet:10.0 |
| appsettings.json             | Default config: Redis connection string, Kestrel port              |
| appsettings.Development.json | Overrides for local development                                    |
| BusinessServer.http          | HTTP request samples for manual endpoint testing                   |

### Controllers/

| File                 | Responsibility                                                           |
| -------------------- | ------------------------------------------------------------------------ |
| PinController.cs     | POST /api/pin/toggle -- main endpoint; delegates to IPinDispatchService  |
| DevicesController.cs | CRUD endpoints for the device registry; uses IDeviceRegistryService      |
| UsersController.cs   | CRUD + invite + role management for the user registry; uses IUserService |
| EchoController.cs    | GET/POST /api/echo -- connectivity test endpoint                         |

### Services/

| File                      | Responsibility                                                            |
| ------------------------- | ------------------------------------------------------------------------- |
| IPinDispatchService.cs    | Interface contract for PinDispatchService                                 |
| PinDispatchService.cs     | Publishes PinCommandMessage to hub:cmd; registers a TaskCompletionSource  |
|                           | per correlationId; returns result or throws TimeoutException (5 s)        |
| PinEventListener.cs       | BackgroundService: subscribes to hub:evt; on PinEventMessage received,    |
|                           | resolves the matching TaskCompletionSource by CorrelationId               |
| IDeviceRegistryService.cs | Interface contract for DeviceRegistryService                              |
| DeviceRegistryService.cs  | In-memory device registry backed by ConcurrentDictionary                  |
| IUserService.cs           | Interface contract for UserService                                        |
| UserService.cs            | In-memory user registry; first /start becomes Admin automatically;        |
|                           | subsequent registrations require a prior invite from Admin/DedicatedAdmin |

## User and role system

Roles (from Protocol spec):

| Role           | Permissions                                        |
| -------------- | -------------------------------------------------- |
| User           | Send device requests; read device info             |
| DedicatedAdmin | User + CRUD devices + CRUD users + invite users    |
| Admin          | DedicatedAdmin + grant/revoke DedicatedAdmin roles |

Registration flow:
1. First user ever sends /start -> registered as Admin.
2. Admin/DedicatedAdmin invites a user by Telegram ID (POST /api/users/invite).
3. Invited user sends any message -> auto-registered as User (POST /api/users/register).

All user and role logic lives exclusively in BusinessServer (IUserService / UserService).
TelegramServer only calls the HTTP API and renders the result.

## Request flow for POST /api/pin/toggle

```
TelegramServer -> POST /api/pin/toggle {pin:8}
  -> PinController -> IPinDispatchService.TogglePinAsync(pin=8)
    -> generate correlationId (GUID)
    -> register TCS in pendingRequests[correlationId]
    -> Redis PUBLISH hub:cmd  PinCommandMessage{correlationId, pin=8}
    -> await TCS with 5 s timeout
      <- PinEventListener receives hub:evt  PinEventMessage{correlationId, pin=8, state=1}
      <- TCS.SetResult(state=1)
    <- TogglePinAsync returns PinToggleResponse{pin=8, state=1}
  <- HTTP 200  {pin:8, state:1}
```

## Configuration

| Key                                 | Default        | Description                      |
| ----------------------------------- | -------------- | -------------------------------- |
| Redis:ConnectionString              | redis:6379     | Redis server address             |
| ASPNETCORE_HTTP_PORTS               | 8080           | Kestrel port (in container)      |
| ConnectionStrings:DefaultConnection | (postgres URL) | PostgreSQL (reserved, OPEN DEC.) |

## Interactions and constraints

- PinDispatchService and PinEventListener share one pendingRequests dictionary; access must be
  thread-safe (ConcurrentDictionary with TryAdd/TryRemove).
- If UartLS does not respond within 5 seconds, the TCS times out and the client receives a 504.
- BusinessServer has no knowledge of Telegram or UART; its boundary is HTTP and Redis only.
- DeviceRegistryService is in-memory; restarting the container clears the registry.
  OPEN DECISION: persist via PostgreSQL.

## Extension points

- New command type: add Dto + Messages in Common, a method on IPinDispatchService, a new action
  or controller in Controllers/.
- Persistence: connect PostgreSQL for device registry and event history.
- Auth: add authentication middleware before controllers.

## Relation to parent folder

BusinessServer is the central service in Hub.
It is the only component that sees both the HTTP boundary (TelegramServer) and the Redis boundary
(UartLS). Runs as a container via docker-compose.yml; port 8080 is published within hub-net.
