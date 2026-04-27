/* main.c — SwarmLibrary demo application.
 * Blinks the built-in LED to verify that swarm_library is correctly linked.
 * Seeed XIAO ESP32C3 built-in LED: GPIO 10 (active-HIGH). */

#include "swarm_library.h"

#define DEMO_LED_GPIO 10

void app_main(void)
{
    swarm_blink_init(DEMO_LED_GPIO);
    swarm_blink_set_period_ms(500);
    swarm_blink_start();
}