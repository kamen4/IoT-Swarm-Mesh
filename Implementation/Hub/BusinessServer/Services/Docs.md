# Services

## Purpose and Boundary

All service interfaces and their implementations for BusinessServer.
Services encapsulate business logic and infrastructure coordination.
Controllers and other consumers depend on interfaces, not concrete classes.

## Files

| Interface                 | Implementation           | Role                                                                               |
| ------------------------- | ------------------------ | ---------------------------------------------------------------------------------- |
| IPinDispatchService.cs    | PinDispatchService.cs    | Publishes PIN_TOGGLE to Redis hub:cmd; waits for response via TaskCompletionSource |
| IPinEventListener.cs      | PinEventListener.cs      | BackgroundService; subscribes to Redis hub:evt and resolves pending TCS entries    |
| IDeviceRegistryService.cs | DeviceRegistryService.cs | In-memory store of registered IoT devices (DeviceInfo records)                     |
| IUserService.cs           | UserService.cs           | In-memory user store; enforces first-user-gets-Admin rule                          |

## Interactions and Constraints

- Services must not reference the Telegram layer or any Telegram SDK types.
- Services must not reference HTTP controllers directly.
- PinDispatchService and PinEventListener are tightly coupled via a shared ConcurrentDictionary of TCS keyed by CorrelationId.
- PinEventListener runs as a hosted BackgroundService; must not block the DI-resolved thread.
- IUserService enforces that the first registered user automatically receives the Admin role.
- No direct serial port or UART access; that is the responsibility of UartLS.

## Relation to Parent Folder

Registered in Program.cs via AddSingleton or AddHostedService.
Controllers receive service interfaces through constructor injection.
PinEventListener must be registered as both IHostedService and IPinEventListener (or AddHostedService with a factory) so the same instance is injected into PinDispatchService.
