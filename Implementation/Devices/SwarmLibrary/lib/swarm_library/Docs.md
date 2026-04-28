# swarm_library

## Purpose

The swarm_library ESP-IDF component. Provides reusable firmware modules for all
IoT Swarm Mesh device projects. Currently contains the swarm_blink module.

## Module: swarm_blink

A GPIO toggle FreeRTOS task with a configurable period.

API summary:
- swarm_blink_init(gpio_num)        -- configure the target GPIO pin
- swarm_blink_set_period_ms(ms)     -- set the toggle interval in milliseconds
- swarm_blink_start()               -- create and start the FreeRTOS task
- swarm_blink_stop()                -- delete the running task

## Files

| File           | Description                                                             |
| -------------- | ----------------------------------------------------------------------- |
| swarm_blink.c  | Blink task implementation                                               |
| CMakeLists.txt | ESP-IDF component descriptor (registers source files and include paths) |
| library.json   | PlatformIO component manifest (name, version, build flags)              |
| include/       | Public header directory                                                 |

## Relation

- Imported by GatewayDevice, RelayDevice, and ButtonDevice via lib_deps in their platformio.ini.
- The demo application in SwarmLibrary/src/main.c shows the canonical usage pattern.
