# GatewayDevice

## Purpose and boundary

GatewayDevice is an ESP32-C3 firmware that bridges Hub UART commands to the ESP-NOW mesh.
The current implementation is a stub: it handles UART command parsing and GPIO toggling but
does not yet transmit to the mesh.

Boundary: this project contains only firmware source (src/main.c). Protocol framing is defined
by the UART wire protocol; mesh transport is not yet implemented.

## Hardware

| Property    | Value                                      |
| ----------- | ------------------------------------------ |
| Board       | Seeed XIAO ESP32-C3                        |
| USB serial  | CDC-ACM -> /dev/ttyACM* on Linux           |
| Stable path | /dev/ttyGATEWAY via udev rule on host      |
| Driver      | usb_serial_jtag (ESP-IDF, not legacy UART) |
| Test GPIO   | GPIO 8                                     |

## UART wire protocol

| Direction     | Line format        | Example        |
| ------------- | ------------------ | -------------- |
| Hub -> Device | PIN_TOGGLE:{pin}\n | PIN_TOGGLE:8\n |
| Device -> Hub | PIN_STATE:{pin}:{0 | 1}\n           | PIN_STATE:8:1\n |

Line terminator: LF (0x0A). All other bytes are buffered until LF.

## Boot sequence

1. usb_serial_jtag driver is installed with 1024-byte RX/TX ring buffers.
2. GPIO 8 is configured as output, initial level 0.
3. Device sends "READY\n" to signal readiness to Hub.
4. gateway_task is spawned (stack: 4096 bytes, priority: tskIDLE_PRIORITY + 1).

## Stack notes

Receive buffer (rx), line accumulator (line), and response buffer (resp) are all declared as
static locals inside gateway_task. This keeps them in BSS/data rather than on the 4096-byte
task stack, preventing stack overflow.

## Subfolders

| Folder   | Purpose                                 |
| -------- | --------------------------------------- |
| src/     | Firmware source files (see src/Docs.md) |
| include/ | Project-level public headers (empty)    |
| lib/     | Project-level private libraries (empty) |
| test/    | Placeholder for device tests            |

## Files

| File                         | Purpose                                        |
| ---------------------------- | ---------------------------------------------- |
| CMakeLists.txt               | Top-level CMake for IDF component registration |
| platformio.ini               | PlatformIO build/upload configuration          |
| sdkconfig.seeed_xiao_esp32c3 | Board-specific SDK configuration               |

## Interactions and constraints

- Hub/UartLS opens the serial port and writes PIN_TOGGLE commands; GatewayDevice echoes
  PIN_STATE responses.
- The host udev rule maps /dev/ttyACM* -> /dev/ttyGATEWAY for a stable device path.
- usb_serial_jtag driver must not be combined with the legacy uart driver on the same port.

## Relation to parent folder

Parent: Implementation/Devices/.
GatewayDevice is the only device that communicates with Hub; all other devices are standalone.
