# src

## Purpose

Source directory for the ButtonDevice firmware.
Contains main.c, which is the entire application for this example device.

## Firmware Behaviour

- app_main configures GPIO 20 as an input with internal pull-down (button input).
- app_main configures GPIO 8 as a push-pull output (built-in LED, active-LOW).
- Main loop polls GPIO 20 every 10 ms and mirrors the button state to GPIO 8
  (active-LOW mapping: button pressed -> GPIO 8 = 0 = LED on; released -> GPIO 8 = 1 = LED off).

## Files

| File   | Description                                              |
| ------ | -------------------------------------------------------- |
| main.c | Sole source file; contains app_main and the polling loop |

## Relation

- Compiled by PlatformIO using CMakeLists.txt in the project root and in this folder.
- Demonstrates basic GPIO input/output on the Seeed XIAO ESP32-C3 target.
