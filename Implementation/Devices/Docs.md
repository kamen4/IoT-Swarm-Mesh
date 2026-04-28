# Devices

## Purpose and boundary

Devices contains all ESP32-C3 embedded firmware projects for the IoT-Swarm-Mesh system.
Each subfolder is a self-contained PlatformIO / ESP-IDF project targeting the Seeed XIAO
ESP32-C3 board.

Boundary: everything in this folder is firmware that runs on microcontrollers; all server-side
code belongs to Implementation/Hub.

## Subfolders

| Folder          | Purpose                                                        |
| --------------- | -------------------------------------------------------------- |
| GatewayDevice/  | Bridges the Hub UART to the ESP-NOW mesh (currently UART stub) |
| ExampleDevices/ | Example firmwares demonstrating peripheral use patterns        |
| SwarmLibrary/   | Shared ESP-IDF component library used by all device firmwares  |

## Files

| File                 | Purpose |
| -------------------- | ------- |
| (none at this level) | --      |

## Build system

- ESP-IDF 5.x is the underlying SDK.
- PlatformIO is used as the build front-end (platformio.ini in each project).
- Target board: Seeed XIAO ESP32-C3 (sdkconfig.seeed_xiao_esp32c3 in each project).

## UART wire protocol (brief)

GatewayDevice communicates with Hub over USB CDC-ACM using a line-based text protocol:

| Direction     | Format             |
| ------------- | ------------------ |
| Hub -> Device | PIN_TOGGLE:{pin}\n |
| Device -> Hub | PIN_STATE:{pin}:{0 | 1}\n |

Full protocol details: Implementation/Hub/UartLS/Docs.md and Protocol/_docs_v1.0/.

## Interactions and constraints

- SwarmLibrary is referenced as a PlatformIO library dependency from all device projects.
- GatewayDevice is the only device connected to Hub; ExampleDevices are standalone.
- All devices target the same hardware: Seeed XIAO ESP32-C3.

## Relation to parent folder

Parent: Implementation/.
The Hub/ subfolder of Implementation/ is the server-side counterpart.
