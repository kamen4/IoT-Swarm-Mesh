# include

## Purpose

Public header directory for the swarm_library component.
These headers form the public API surface that consumer firmware projects include.

## Files

| File            | Description                                                                       |
| --------------- | --------------------------------------------------------------------------------- |
| swarm_library.h | Umbrella header; include this single file in application code to get the full API |
| swarm_blink.h   | Blink module public API: init, set_period_ms, start, stop                         |
| swarm_connect.h | Onboarding captive-portal API: init, get_connection_string, stop                  |

## Relation

- Included by swarm_blink.c and swarm_connect.c (implementations) and by all consumer
  firmware projects (GatewayDevice, RelayDevice, ButtonDevice).
- Exported automatically by the ESP-IDF CMakeLists.txt INCLUDE_DIRS declaration,
  so no manual include-path configuration is needed in consumer projects.
