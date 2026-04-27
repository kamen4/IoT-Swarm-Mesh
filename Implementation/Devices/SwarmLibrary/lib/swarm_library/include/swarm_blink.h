/* swarm_blink.h — blink module public API.
 *
 * Placeholder module that demonstrates correct library linkage.
 * The blink loop runs in a dedicated FreeRTOS task.
 * Will be superseded by swarm protocol modules as the project evolves. */
#pragma once

#include <stdint.h>

/* Initialise the GPIO as an output. Must be called once before start. */
void swarm_blink_init(int gpio_num);

/* Set the full blink period in milliseconds (default: 500 ms). */
void swarm_blink_set_period_ms(uint32_t period_ms);

/* Start the blink task. Has no effect if the task is already running. */
void swarm_blink_start(void);

/* Stop the blink task and drive the GPIO low. */
void swarm_blink_stop(void);
