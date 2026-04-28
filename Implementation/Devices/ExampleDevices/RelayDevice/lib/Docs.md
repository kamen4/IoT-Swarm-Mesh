# lib

## Purpose

Project-local library directory for the RelayDevice firmware.
Links SwarmLibrary as a build dependency; no additional local components are defined here.

## Dependency

SwarmLibrary is the shared ESP-IDF component used by all device projects.
It is resolved via a direct path reference in platformio.ini lib_deps
(pointing to Implementation/Devices/SwarmLibrary/lib/swarm_library).

## Files

| File   | Description                            |
| ------ | -------------------------------------- |
| README | PlatformIO placeholder (do not delete) |

## Relation

- PlatformIO scans this folder for local components and also resolves the SwarmLibrary path from platformio.ini.
