/**
 * @file ds1302.h
 * @brief Public API for the DS1302 RTC driver.
 *
 * All time values exchanged through this API are decimal integers
 * (BCD conversion is handled internally).
 *
 * IMPORTANT -- persistent timekeeping:
 *   Connect a CR2032 coin cell to DS1302 VBAT (VCC2 pin).  Without it the
 *   oscillator stops when the MCU loses power and time is lost regardless of
 *   any software measures.
 *
 * IMPORTANT -- RST pin options:
 *   Option A (recommended): keep RST wired to an ESP32 GPIO (e.g. GPIO 20)
 *   with a 10 kΩ pull-down to GND.  The pull-down holds RST LOW during boot
 *   before the GPIO is configured, preventing spurious transactions.
 *
 *   Option B: disconnect RST from MCU, tie DS1302 RST to 3.3V via 10 kΩ
 *   pull-up.  Pass rst_gpio = -1 to ds1302_init.  In this case CLK MUST
 *   have a 10 kΩ pull-down to GND to block accidental clock edges at boot.
 *   RST is always HIGH so the DS1302 is always "enabled"; correct operation
 *   depends on CLK being LOW between transactions.
 */
#pragma once
#include <stdbool.h>
#include <stdint.h>

/** Initialise GPIO lines and start the RTC oscillator if it was stopped.
 *  @param rst_gpio  GPIO for the DS1302 RST (CE) pin, OR -1 if RST is
 *                   hardware-tied HIGH (10 kΩ pull-up to VCC).  When -1,
 *                   the CLK line MUST have a 10 kΩ pull-down to GND.
 *  @return true  if the oscillator was halted at boot (time was reset --
 *                no battery or battery is dead).
 *  @return false if the oscillator was already running; time is preserved. */
bool ds1302_init(int rst_gpio, int dat_gpio, int clk_gpio);

/** Read hours, minutes and seconds from the RTC (decimal, not BCD). */
void ds1302_get_time(uint8_t *hour, uint8_t *minute, uint8_t *second);

/** Write hours, minutes and seconds to the RTC.
 *  Temporarily disables write-protect, writes all three registers,
 *  then re-enables write-protect. */
void ds1302_set_time(uint8_t hour, uint8_t minute, uint8_t second);
