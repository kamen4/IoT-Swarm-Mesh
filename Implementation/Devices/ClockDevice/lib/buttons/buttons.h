/**
 * @file buttons.h
 * @brief Public API for the two-button state-machine.
 *
 * Both buttons must be wired between a GPIO and GND.
 * Internal pull-up resistors are enabled; a pressed button reads LOW.
 *
 * Recognised gestures:
 *   - Single click       : press + release before HOLD_MS
 *   - Double/triple click: 2 or 3 clicks within DCLICK_MS of each other
 *   - Hold               : press held for >= HOLD_MS
 *   - Both click / hold  : both buttons pressed simultaneously
 */
#pragma once
#include <stdbool.h>
#include <stdint.h>

/** All events that can be returned by buttons_poll(). */
typedef enum
{
    BTN_NONE = 0,   /**< No event ready */
    BTN_L_CLICK,    /**< Single click  — left  */
    BTN_R_CLICK,    /**< Single click  — right */
    BTN_L_DCLICK,   /**< Double click  — left  */
    BTN_R_DCLICK,   /**< Double click  — right */
    BTN_L_TCLICK,   /**< Triple click  — left  */
    BTN_R_TCLICK,   /**< Triple click  — right */
    BTN_L_HOLD,     /**< Hold >= HOLD_MS — left  */
    BTN_R_HOLD,     /**< Hold >= HOLD_MS — right */
    BTN_BOTH_CLICK, /**< Both buttons released before hold threshold */
    BTN_BOTH_HOLD,  /**< Both buttons held >= HOLD_MS */
} btn_event_t;

/** Configure GPIO lines with internal pull-up and reset internal state.
 *  @param gpio_l  GPIO for the left  button (second contact to GND).
 *  @param gpio_r  GPIO for the right button (second contact to GND). */
void buttons_init(int gpio_l, int gpio_r);

/** Poll button state.  Call every ~50 ms from the main loop.
 *  Returns BTN_NONE when no event is ready. */
btn_event_t buttons_poll(void);
