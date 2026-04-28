# Implementation

## Purpose and boundary

Implementation contains all source code for the IoT-Swarm-Mesh project.
It is divided into two top-level subfolders:

- Hub/ -- server-side .NET services running in Docker containers.
- Devices/ -- embedded firmware for ESP32-C3 microcontrollers flashed with ESP-IDF/PlatformIO.

The two sides are connected by a physical UART link: GatewayDevice (in Devices/) connects to
the host machine running the Hub via USB CDC-ACM. UartLS (in Hub/) opens the serial port and
exchanges pin-command / pin-state messages with GatewayDevice.

The wire protocol is defined in Protocol/_docs_v1.0/; see Hub/UartLS/Docs.md for the
implementation-level wire protocol table.

## Subfolders

| Folder   | Purpose                                                              |
| -------- | -------------------------------------------------------------------- |
| Hub/     | Server-side: .NET containers, Redis, InfluxDB, Grafana, Telegram bot |
| Devices/ | Embedded firmware: ESP32-C3 devices, shared SwarmLibrary component   |

## Files

| File                   | Purpose                                              |
| ---------------------- | ---------------------------------------------------- |
| DEVICES.code-workspace | VS Code multi-root workspace for all device projects |

## Interactions and constraints

- Hub and Devices are independent build systems (Docker / PlatformIO); they share no source.
- Communication at runtime is exclusively via the UART wire protocol over USB CDC-ACM.
- Protocol specification: Protocol/_docs_v1.0/ (repository root).
- The stable device path /dev/ttyGATEWAY is set up on the host via a udev rule; see
  Devices/GatewayDevice/Docs.md for details.

## Relation to parent folder

This folder is a direct child of the repository root (IoT-Swarm-Mesh/).
The repository root also contains:

- Protocol/ -- formal protocol specification and simulation models.
- SimModel/ -- Python simulation models.
- Docs/     -- project-level development log.
