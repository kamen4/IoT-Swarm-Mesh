/**
 * @file led_ring.h
 * @brief Public API for the 12-LED WS2812B ring driver.
 *
 * Uses the espressif/led_strip IDF component (RMT back-end) declared
 * in src/idf_component.yml.
 */
#pragma once
#include <stdbool.h>
#include <stdint.h>

/** Initialise the RMT-backed LED strip and clear all pixels.
 *  @param led_gpio  GPIO connected to the ring DI pin.
 *  @param count     Total number of LEDs in the ring (typically 12).
 *  @param offset    Index shift so that position 0 maps to physical 12 o'clock. */
void led_ring_init(int led_gpio, int count, int offset);

/** Render clock hands on the ring.
 *  Red pixel  = hour hand position   : (hour % 12 + offset) % count
 *  Green pixel = minute hand position : (minute / 5 + offset) % count
 *  Overlapping position shows yellow (r + g mixed).
 *  @param brightness  Pixel brightness 0–255. */
void led_ring_show_clock(uint8_t hour, uint8_t minute, uint8_t brightness);

/** Render a bar graph showing val/max_val fraction of LEDs lit.
 *  Used by the settings UI to visualise the current setting value.
 *  @param r,g,b  Colour of lit pixels. */
void led_ring_show_bar(uint8_t val, uint8_t max_val,
                       uint8_t r, uint8_t g, uint8_t b);

/** Run a roulette animation: spin for several full turns then decelerate and
 *  stop at a random position.  The winning pixel is left lit; the caller
 *  handles result sound and blink effects.
 *  Blocking call; returns once the ball has stopped.
 *  @param brightness  Pixel brightness.
 *  @param tick_cb     Called on each spin step (short beep).  NULL = silent.
 *  @return  true  if the winning pocket is red (odd index).
 *           false if the winning pocket is blue (even index). */
bool led_ring_roulette(uint8_t brightness, void (*tick_cb)(void));

/** Set every LED in the ring to one colour and refresh. */
void led_ring_set_all(uint8_t r, uint8_t g, uint8_t b);
