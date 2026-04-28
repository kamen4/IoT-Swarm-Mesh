# ButtonDevice

## Purpose and boundary

ButtonDevice is an example ESP32-C3 firmware that maps a button input (GPIO 20) to the
built-in LED output (GPIO 8). While the button is pressed the LED turns on; when released it
turns off. It demonstrates GPIO input with pull-down and active-LOW LED output.

Boundary: this is an example/development firmware only. It does not implement any mesh or UART
protocol.

## Hardware

| Property       | Value                             |
| -------------- | --------------------------------- |
| Board          | Seeed XIAO ESP32-C3               |
| Button pin     | GPIO 20 (input, pull-down)        |
| LED pin        | GPIO 8 (built-in LED, active-LOW) |
| Polling period | 10 ms                             |

LED logic: active-LOW -- level 0 = LED on, level 1 = LED off.
Button logic: GPIO_PULLDOWN_ONLY; pressed = level 1, released = level 0.

## Subfolders

| Folder   | Purpose                                         |
| -------- | ----------------------------------------------- |
| src/     | main.c -- firmware entry point and polling loop |
| include/ | Project-level public headers (empty)            |
| lib/     | Links SwarmLibrary component                    |
| test/    | Placeholder for device tests                    |

## Files

| File                         | Purpose                                        |
| ---------------------------- | ---------------------------------------------- |
| CMakeLists.txt               | Top-level CMake for IDF component registration |
| platformio.ini               | PlatformIO build/upload configuration          |
| sdkconfig.seeed_xiao_esp32c3 | Board-specific SDK configuration               |

## Interactions and constraints

- Depends on SwarmLibrary (swarm_library component) via PlatformIO lib_deps.
- No UART or mesh protocol; entirely self-contained after flash.
- GPIO 20 must have an external button connecting to 3.3V; the internal pull-down is enabled.
- GPIO 8 is the built-in LED; see hardware note above for active-LOW wiring.

## Relation to parent folder

Parent: Implementation/Devices/ExampleDevices/.
RelayDevice/ (sibling) demonstrates timer-driven output rather than input-driven output.
