# SwarmLibrary

## Purpose and boundary

SwarmLibrary is a shared ESP-IDF component library used by all device firmwares in
Implementation/Devices/. It provides common utilities and will grow to contain the ESP-NOW
mesh protocol implementation.

Currently the library contains the swarm_blink module as a placeholder that demonstrates the
correct component structure.

Boundary: this project provides a reusable PlatformIO library (lib/swarm_library/). The src/
folder contains a standalone example/test application that exercises the library.

## Library path

lib/swarm_library/

## Public headers

| Header          | Purpose                                              |
| --------------- | ---------------------------------------------------- |
| swarm_library.h | Umbrella header -- include this in consumer firmware |
| swarm_blink.h   | GPIO blink task API                                  |

## swarm_blink module

swarm_blink provides a FreeRTOS task that blinks a configurable GPIO at a configurable period.
It is a placeholder; the same pattern will be used for mesh protocol tasks.

API (swarm_blink.h):

| Function                               | Description                                      |
| -------------------------------------- | ------------------------------------------------ |
| swarm_blink_init(int gpio_num)         | Configure GPIO as output; call once before start |
| swarm_blink_set_period_ms(uint32_t ms) | Set full blink period (default 500 ms)           |
| swarm_blink_start(void)                | Start the blink task (no-op if already running)  |
| swarm_blink_stop(void)                 | Stop the task and drive GPIO low                 |

## Subfolders

| Folder             | Purpose                                          |
| ------------------ | ------------------------------------------------ |
| lib/               | PlatformIO library root                          |
| lib/swarm_library/ | swarm_library ESP-IDF component                  |
| src/               | Example/test application that uses swarm_library |
| include/           | Project-level public headers (empty)             |
| test/              | Placeholder for component tests                  |

## Files

### Root

| File                         | Purpose                                        |
| ---------------------------- | ---------------------------------------------- |
| CMakeLists.txt               | Top-level CMake for IDF component registration |
| platformio.ini               | PlatformIO build/upload configuration          |
| sdkconfig.seeed_xiao_esp32c3 | Board-specific SDK configuration               |

### lib/swarm_library/

| File                    | Purpose                                     |
| ----------------------- | ------------------------------------------- |
| CMakeLists.txt          | Registers swarm_library as an IDF component |
| library.json            | PlatformIO library descriptor               |
| swarm_blink.c           | swarm_blink task implementation             |
| include/swarm_library.h | Umbrella public header                      |
| include/swarm_blink.h   | swarm_blink public API                      |

## Interactions and constraints

- Consumers reference swarm_library via lib_deps in their platformio.ini.
- The umbrella header swarm_library.h should be included by firmware; individual module
  headers (swarm_blink.h) are also available for selective inclusion.
- Future: ESP-NOW mesh protocol tasks will be added as additional modules alongside swarm_blink.

## Relation to parent folder

Parent: Implementation/Devices/.
GatewayDevice/ and ExampleDevices/ (RelayDevice, ButtonDevice) all depend on SwarmLibrary.
