/**
 * @file tm1637.h
 * @brief Public API for the TM1637 4-digit 7-segment display driver.
 *
 * Communication is bit-banged over a two-wire open-drain bus.
 * Both CLK and DIO lines require internal or external pull-up resistors.
 */
#pragma once
#include <stdint.h>

/** Initialise the display.  Must be called once before any other function.
 *  Configures both GPIO lines as open-drain outputs with pull-up enabled. */
void tm1637_init(int clk_gpio, int dio_gpio);

/* Показать время HH:MM с двоеточием */
void tm1637_show_time(uint8_t h, uint8_t m);

/* Показать «--XX» (правые два разряда, левые гасятся) */
void tm1637_show_right(uint8_t val);

/* Показать «XX--» (левые два разряда, правые гасятся) */
void tm1637_show_left(uint8_t val);

/* Отправить 4 произвольных сегмента (seg[1] бит 7 = двоеточие) */
void tm1637_send_raw(uint8_t seg[4]);
