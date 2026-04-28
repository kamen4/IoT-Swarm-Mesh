# Controllers

## Purpose and Boundary

ASP.NET Core MVC controllers that define the HTTP API surface of BusinessServer.
Each controller handles routing, input validation, and HTTP response shaping only.
No business logic is permitted here; all logic is delegated to the service layer.

## Files

| File                 | Route           | Methods                                | Notes                               |
| -------------------- | --------------- | -------------------------------------- | ----------------------------------- |
| PinController.cs     | /api/pin/toggle | POST                                   | Delegates to IPinDispatchService    |
| EchoController.cs    | /api/echo       | GET, POST                              | Diagnostic echo endpoint            |
| DevicesController.cs | /api/devices    | GET, POST, PUT, DELETE                 | CRUD for registered IoT devices     |
| UsersController.cs   | /api/users      | GET, POST, PUT, DELETE + invite + role | User management and role assignment |

## Interactions and Constraints

- Controllers must not contain business logic.
- All operations are delegated to service-layer interfaces injected via constructor.
- Input models come from Common/Dto; response models are also from Common/Dto.
- Authentication/authorization attributes are applied at the controller or action level.
- No direct Redis, database, or serial port access.

## Relation to Parent Folder

Sits inside BusinessServer.
Routes are registered automatically by ASP.NET Core when controllers are discovered.
Program.cs calls AddControllers() and MapControllers() to enable routing.
