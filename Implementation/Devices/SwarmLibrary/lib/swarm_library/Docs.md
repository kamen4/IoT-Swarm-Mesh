# swarm_library

## Purpose

The swarm_library ESP-IDF component. Provides reusable firmware modules for all
IoT Swarm Mesh device projects. Contains swarm_blink (LED demo) and swarm_connect
(onboarding captive portal).

## Module: swarm_blink

A GPIO toggle FreeRTOS task with a configurable period.

API summary:
- swarm_blink_init(gpio_num)        -- configure the target GPIO pin
- swarm_blink_set_period_ms(ms)     -- set the toggle interval in milliseconds
- swarm_blink_start()               -- create and start the FreeRTOS task
- swarm_blink_stop()                -- delete the running task

## Module: swarm_connect

Onboarding captive portal: Wi-Fi Soft-AP + DNS redirect server + HTTP server.
When a user connects to the device AP, the OS captive-portal detector fires and
opens a mini-browser that is automatically redirected to the device info page.
The page displays the device MAC address and the CONNECTION_STRING (per the
protocol onboarding spec) with a one-tap copy button.

CONNECTION_STRING format (Protocol/_docs_v1.0/00-glossary.md):
  <AA:BB:CC:DD:EE:FF>:<base64(SHA256(CONNECTION_KEY))>

API summary:
- swarm_connect_init()                         -- start AP + DNS + HTTP portal
- swarm_connect_get_connection_string(buf, len) -- read the CONNECTION_STRING
- swarm_connect_stop()                         -- stop portal and tear down AP

## Files

| File            | Description                                                             |
| --------------- | ----------------------------------------------------------------------- |
| swarm_blink.c   | Blink task implementation                                               |
| swarm_connect.c | Captive portal implementation (AP, DNS server, HTTP server)             |
| CMakeLists.txt  | ESP-IDF component descriptor (registers source files and include paths) |
| library.json    | PlatformIO component manifest (name, version, build flags)              |
| include/        | Public header directory                                                 |

## Relation

- Imported by GatewayDevice, RelayDevice, and ButtonDevice via lib_deps in their platformio.ini.
- The demo application in SwarmLibrary/src/main.c shows the canonical usage pattern.
- swarm_connect follows Protocol/_docs_v1.0/algorithms/01-onboarding.md.
