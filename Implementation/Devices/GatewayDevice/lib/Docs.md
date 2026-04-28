# lib

## Purpose

Project-local library components directory for the GatewayDevice firmware.
Currently empty (contains only the PlatformIO-generated README placeholder).

## Shared Library

SwarmLibrary is the shared component used across all device projects.
It lives at Implementation/Devices/SwarmLibrary/ and is referenced from platformio.ini via a direct path in lib_deps.

## Files

| File   | Description                            |
| ------ | -------------------------------------- |
| README | PlatformIO placeholder (do not delete) |

## Relation

- PlatformIO resolves lib_deps from this folder first, then from the SwarmLibrary path declared in platformio.ini.
- Place device-specific local components here if the gateway firmware ever requires them.
