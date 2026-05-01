/**
 * @file clock_ui.h
 * @brief Public API for the clock UI state-machine.
 *
 * Orchestrates all hardware drivers (tm1637, led_ring, buzzer, ds1302)
 * in response to button events.  main.c calls clock_ui_tick() every
 * ~50 ms and checks the returned mode to decide what to render in the
 * normal clock view.
 */
#pragma once
#include "buttons.h"
#include <stdbool.h>
#include <stdint.h>

/** All UI modes managed by the state-machine. */
typedef enum
{
    UI_MODE_CLOCK = 0,   /**< Normal clock display */
    UI_MODE_SET_H,       /**< Editing hours   (left digits blink) */
    UI_MODE_SET_M,       /**< Editing minutes (right digits blink) */
    UI_MODE_SET_BRIGHT,  /**< Editing LED ring brightness */
    UI_MODE_SET_QUIET_F, /**< Editing quiet-time start hour */
    UI_MODE_SET_QUIET_T, /**< Editing quiet-time end hour */
    UI_MODE_ROULETTE,    /**< Roulette animation in progress */
} ui_mode_t;

/** User-configurable settings exposed to main.c (read-only via pointer). */
typedef struct
{
    uint8_t brightness; /**< LED ring brightness 1–64 */
    uint8_t quiet_from; /**< Start of silent period (0–23) */
    uint8_t quiet_to;   /**< End   of silent period (0–23) */
    bool sound_mute;    /**< Manual mute flag */
} ui_settings_t;

/** Initialise the UI with default brightness and quiet-time window.
 *  Must be called once after all hardware drivers are initialised. */
void clock_ui_init(uint8_t brightness, uint8_t quiet_from, uint8_t quiet_to);

/** Main update function — call every ~50 ms from the main loop.
 *  @param hour    Current hour   from RTC (0–23).
 *  @param minute  Current minute from RTC (0–59).
 *  @param ev      Latest button event (BTN_NONE if none).
 *  @return        Current ui_mode_t. */
ui_mode_t clock_ui_tick(uint8_t hour, uint8_t minute, btn_event_t ev);

/** Return a read-only pointer to the current settings struct. */
const ui_settings_t *clock_ui_settings(void);
