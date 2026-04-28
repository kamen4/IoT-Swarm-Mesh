# src

## Purpose

Source directory for the SwarmLibrary demo firmware.
Contains main.c, which is a standalone example application that exercises the SwarmLibrary component.
This is NOT the library implementation itself.

## Firmware Behaviour

app_main performs the following calls to demonstrate correct library usage:

1. swarm_blink_init(GPIO_NUM_8)    -- attach the blink module to the built-in LED pin
2. swarm_blink_set_period_ms(500)  -- configure 500 ms toggle period
3. swarm_blink_start()             -- start the FreeRTOS blink task

## Files

| File   | Description                  |
| ------ | ---------------------------- |
| main.c | Demo application entry point |

## Relation

- Compiled by PlatformIO as a full ESP-IDF application targeting the XIAO ESP32-C3.
- The actual library code lives in lib/swarm_library/.
- Consumer device projects (GatewayDevice, RelayDevice, ButtonDevice) replicate this usage pattern.
