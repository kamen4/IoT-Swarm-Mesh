# GatewayDevice/src

## Purpose and boundary

This folder contains the sole firmware source file for GatewayDevice: main.c.
All application logic lives in main.c; there are no additional source files.

## Files

| File   | Purpose                                                 |
| ------ | ------------------------------------------------------- |
| main.c | Complete firmware: driver init, GPIO setup, UART bridge |

## main.c structure

### app_main

1. Installs the usb_serial_jtag driver (1024-byte RX/TX ring buffers).
2. Resets and configures GPIO 8 as output; sets initial level to 0.
3. Writes "READY\n" to the serial port to signal boot completion to Hub.
4. Spawns gateway_task with a 4096-byte stack.

### gateway_task

- Reads up to 255 bytes at a time from the USB serial JTAG ring buffer (20 ms timeout).
- Accumulates bytes into a static line buffer.
- On '\r': byte is discarded.
- On '\n': line is null-terminated and checked against "PIN_TOGGLE:8".
  - If matched: toggles s_pin_state (0->1->0), calls gpio_set_level, writes PIN_STATE response.
  - Line buffer is reset regardless of match.
- Non-newline bytes are appended up to BUF_SIZE - 1 to prevent overflow.

### Static buffer layout

| Variable    | Size (bytes) | Location           | Purpose                |
| ----------- | ------------ | ------------------ | ---------------------- |
| rx          | 256          | BSS (static local) | Raw read buffer        |
| line        | 256          | BSS (static local) | Line accumulator       |
| resp        | 256          | BSS (static local) | Response format buffer |
| s_pin_state | 4            | BSS (file-scope)   | Current GPIO 8 state   |

## Interactions and constraints

- gateway_task is the only task that reads the USB serial JTAG port.
- Static buffers are safe because gateway_task is a singleton (no re-entrancy).
- Full UART protocol: see GatewayDevice/Docs.md and Hub/UartLS/Docs.md.

## Relation to parent folder

Parent: GatewayDevice/.
This folder contains all source files; the parent CMakeLists.txt registers src/ as the main
component source directory.
