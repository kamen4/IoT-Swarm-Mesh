# RelayDevice

## Purpose and boundary

RelayDevice is an example ESP32-C3 firmware that blinks the built-in LED (GPIO 8) at a fixed
500 ms period (250 ms on / 250 ms off). It demonstrates a simple GPIO output loop using the
SwarmLibrary dependency.

Boundary: this is an example/development firmware only. It does not implement any mesh or UART
protocol.

## Hardware

| Property     | Value                          |
| ------------ | ------------------------------ |
| Board        | Seeed XIAO ESP32-C3            |
| LED pin      | GPIO 8 (built-in LED)          |
| Logic        | Active-LOW (0 = on, 1 = off)   |
| Blink period | 500 ms (250 ms on, 250 ms off) |

Note: the built-in LED on the Seeed XIAO ESP32-C3 is wired active-LOW. Writing level 0 turns
the LED on; writing level 1 turns it off.

## Subfolders

| Folder   | Purpose                                       |
| -------- | --------------------------------------------- |
| src/     | main.c -- firmware entry point and blink loop |
| include/ | Project-level public headers (empty)          |
| lib/     | Links SwarmLibrary component                  |
| test/    | Placeholder for device tests                  |

## Files

| File                         | Purpose                                        |
| ---------------------------- | ---------------------------------------------- |
| CMakeLists.txt               | Top-level CMake for IDF component registration |
| platformio.ini               | PlatformIO build/upload configuration          |
| sdkconfig.seeed_xiao_esp32c3 | Board-specific SDK configuration               |

## Interactions and constraints

- Depends on SwarmLibrary (swarm_library component) via PlatformIO lib_deps.
- No UART or mesh protocol; entirely self-contained after flash.
- GPIO 8 is shared with the built-in LED; do not drive it from external hardware without
  verifying current limits.

## Relation to parent folder

Parent: Implementation/Devices/ExampleDevices/.
ButtonDevice/ (sibling) demonstrates input (button) rather than timer-driven output (blink).
