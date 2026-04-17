# Roles And Actors

## Human roles (from reference/users.md)

- User:
  - Uses authorized commands and reads data according to configured permissions.
- DedicatedAdmin:
  - Administrative rights within assigned scope.
- Admin:
  - Full administrative control over system-wide configuration and governance.

## System actors

- Server:
  - Authoritative control logic and state management.
- Gateway device:
  - UART/ESP-NOW bridge and protocol forwarding participant.
- Device library:
  - Protocol implementation surface for endpoint devices.
- Endpoint device firmware:
  - Uses library to expose interaction protocol and runtime behavior.
