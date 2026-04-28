# lib

## Purpose

Contains the swarm_library PlatformIO/ESP-IDF component.
This is the canonical location of the shared library used by all device firmware projects.

## Structure

    lib/
    +-- swarm_library/   <- self-contained ESP-IDF component

## Files

| File/Folder    | Description                                             |
| -------------- | ------------------------------------------------------- |
| swarm_library/ | The SwarmLibrary component (source, headers, manifests) |

## Relation

- Other device projects (GatewayDevice, RelayDevice, ButtonDevice) declare this path in
  their platformio.ini lib_deps to pull in the component at build time.
- PlatformIO resolves lib/ subfolders automatically, so the component is discoverable
  without additional configuration when building the SwarmLibrary project itself.
