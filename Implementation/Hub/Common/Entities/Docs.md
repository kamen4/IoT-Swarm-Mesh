# Entities

## Purpose and Boundary

Domain entity types used across Hub services.
These represent core domain objects with identity, distinct from DTOs (which are HTTP transport types) and messages (which are Redis payloads).

## Files

| File           | Type        | Fields                                                   |
| -------------- | ----------- | -------------------------------------------------------- |
| DeviceInfo.cs  | DeviceInfo  | DeviceId, DeviceName, DeviceType, IsOnline               |
| MeshMessage.cs | MeshMessage | Generic mesh packet placeholder; reserved for future use |

## Interactions and Constraints

- No logic; data containers only.
- DeviceInfo is stored in IDeviceRegistryService and returned in /api/devices responses (mapped to a DTO if needed).
- MeshMessage is a placeholder and must not be used in production code until the mesh packet format is finalized.
- Do not add ORM annotations (e.g., Entity Framework attributes) unless a database backend is introduced.

## Relation to Parent Folder

Sits inside Common, which is referenced by BusinessServer, TelegramServer, and UartLS.
BusinessServer/Services is the primary consumer of DeviceInfo.
