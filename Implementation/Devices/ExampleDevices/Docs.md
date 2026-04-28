# ExampleDevices

## Purpose and boundary

ExampleDevices contains example ESP32-C3 firmware projects that demonstrate peripheral use
patterns for the IoT-Swarm-Mesh project. These are not production devices; they serve as
development and integration examples.

Boundary: devices in this folder are standalone -- they do not communicate with Hub and are
not part of the mesh protocol. They exist to validate hardware bring-up and library linkage.

## Subfolders

| Folder        | Purpose                                                   |
| ------------- | --------------------------------------------------------- |
| RelayDevice/  | Blinks GPIO 8 at a 500 ms period (active-LOW LED example) |
| ButtonDevice/ | Maps a button on GPIO 20 to the LED on GPIO 8             |

## Files

| File                 | Purpose |
| -------------------- | ------- |
| (none at this level) | --      |

## Interactions and constraints

- Both devices use SwarmLibrary (lib/swarm_library/) as a PlatformIO dependency.
- Neither device communicates with Hub or uses the UART wire protocol.
- Target board: Seeed XIAO ESP32-C3 (same as GatewayDevice).

## Relation to parent folder

Parent: Implementation/Devices/.
GatewayDevice/ (sibling) is the only device that communicates with Hub.
SwarmLibrary/ (sibling) is the shared component used by both example devices.
