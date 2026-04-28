# src

## Purpose

Source directory for the RelayDevice firmware.
Contains main.c, which is the entire application for this example device.

## Firmware Behaviour

- app_main configures GPIO 8 as a push-pull output.
- Enters an infinite loop that toggles GPIO 8 every 500 ms.
- GPIO 8 is the built-in LED on the Seeed XIAO ESP32-C3; it is active-LOW (0 = on, 1 = off).

## Files

| File   | Description                                            |
| ------ | ------------------------------------------------------ |
| main.c | Sole source file; contains app_main and the blink loop |

## Relation

- Compiled and linked by PlatformIO using CMakeLists.txt in the project root and in this folder.
- Demonstrates a minimal ESP-IDF application on the XIAO ESP32-C3 target.
