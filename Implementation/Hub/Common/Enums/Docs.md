# Enums

## Purpose and Boundary

Shared enumeration types used across Hub services.
Centralizing enums in Common prevents duplication and keeps values consistent between projects.

## Files

| File        | Type     | Values                            |
| ----------- | -------- | --------------------------------- |
| UserRole.cs | UserRole | User=0, DedicatedAdmin=1, Admin=2 |

## Interactions and Constraints

- UserRole is used in HTTP DTOs (UserDto, SetRoleRequest) and in the BusinessServer service layer (IUserService).
- Numeric values are part of the serialized contract; do not reorder or renumber without a migration plan.
- TelegramServer reads UserRole from API responses via Common/Dto; MenuBuilder branches on it to build role-specific keyboards.
- No additional logic or extension methods should be placed in this folder.

## Relation to Parent Folder

Sits inside Common, which is referenced by BusinessServer, TelegramServer, and UartLS.
Any enum value change is a breaking change for all consumers.
