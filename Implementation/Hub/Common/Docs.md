# Common

## Purpose and boundary

Common is a shared .NET class library with no application logic.
It provides typed contracts for the HTTP layer (Dto), the Redis bus (Messages), and domain
entities (Entities). All other Hub projects (BusinessServer, TelegramServer, UartLS) reference
Common; Common does not reference any of them.

## Subfolders

| Folder    | Purpose                                                               |
| --------- | --------------------------------------------------------------------- |
| Dto/      | HTTP request and response types between TelegramServer/BusinessServer |
| Messages/ | Redis message payloads for channels hub:cmd and hub:evt               |
| Entities/ | Domain entity types (devices, mesh messages)                          |

## Files

### Dto/

| File                 | Responsibility                                                        |
| -------------------- | --------------------------------------------------------------------- |
| EchoRequest.cs       | Request body for the test echo endpoint                               |
| EchoResponse.cs      | Response body for the test echo endpoint                              |
| PinToggleRequest.cs  | Request body for POST /api/pin/toggle; contains the pin number        |
| PinToggleResponse.cs | Response body for POST /api/pin/toggle; contains the resulting state  |

### Messages/

| File                 | Responsibility                                                           |
| -------------------- | ------------------------------------------------------------------------ |
| PinCommandMessage.cs | Payload published to Redis channel hub:cmd; fields: CorrelationId, Pin   |
| PinEventMessage.cs   | Payload published to Redis channel hub:evt; fields: CorrelationId, Pin,  |
|                      | State                                                                    |

### Entities/

| File           | Responsibility                                                            |
| -------------- | ------------------------------------------------------------------------- |
| DeviceInfo.cs  | Registered device entity (used by DeviceRegistryService)                  |
| MeshMessage.cs | Generic mesh network message type (reserved for future extension)         |

### Root

| File          | Responsibility                                                      |
| ------------- | ------------------------------------------------------------------- |
| Common.csproj | .NET class library definition; referenced by all Hub projects       |

## Interactions and constraints

- CorrelationId in PinCommandMessage and PinEventMessage is the only mechanism for matching a
  command to its response; must be unique per call (GUID).
- Dto types are used only at the HTTP boundary; they are not published to Redis.
- Messages types are used only at the Redis boundary; they are not serialised into HTTP responses.
- Any change to a Messages type affects both BusinessServer and UartLS simultaneously.
- Entities contain no logic; they are data containers only.

## Extension points

- New command type: add a new Dto pair (request + response) and a new Messages pair (command +
  event); update Docs.md in Common and in the affected service folders.

## Relation to parent folder

Common is a dependency of all other Hub projects.
It is not deployed independently; it is compiled into each service image.
