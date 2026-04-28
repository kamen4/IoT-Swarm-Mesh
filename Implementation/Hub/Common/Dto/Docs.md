# Dto

## Purpose and Boundary

HTTP request and response record types shared between TelegramServer and BusinessServer.
These types define the contract for all HTTP bodies exchanged over the internal REST API.

## Files

| File                 | Types                                                                                | Direction                              |
| -------------------- | ------------------------------------------------------------------------------------ | -------------------------------------- |
| EchoRequest.cs       | EchoRequest                                                                          | Request body for POST /api/echo        |
| EchoResponse.cs      | EchoResponse                                                                         | Response body for /api/echo            |
| PinToggleRequest.cs  | PinToggleRequest                                                                     | Request body for POST /api/pin/toggle  |
| PinToggleResponse.cs | PinToggleResponse                                                                    | Response body for POST /api/pin/toggle |
| UserDtos.cs          | UserDto, RegisterUserRequest, SetRoleRequest, InviteUserRequest, CheckAccessResponse | User management request/response types |

## Interactions and Constraints

- These types are NOT sent over Redis; they are HTTP body and response types only.
- No logic; data containers only (record or class with properties).
- Must be kept in sync between BusinessServer (uses as controller parameter/return types) and TelegramServer (uses via BusinessServerClient).
- Any change to a DTO breaks the HTTP contract between services; both ends must be updated together.
- Serialization uses System.Text.Json defaults unless overridden in Program.cs.

## Relation to Parent Folder

Sits inside Common, which is a shared class library referenced by both BusinessServer and TelegramServer.
Types in this folder are the only HTTP-level contract between those two services.
