/* RelayDevice -- example ESP32-C3 firmware.
 * Blinks the built-in LED on GPIO 8 at a fixed 500 ms period (active-LOW). */

#include "driver/gpio.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "swarm_library.h"

#define BLINK_GPIO GPIO_NUM_8
#define BLINK_PERIOD_MS 500

void app_main(void)
{
    gpio_reset_pin(BLINK_GPIO);
    gpio_set_direction(BLINK_GPIO, GPIO_MODE_OUTPUT);

    while (1)
    {
        gpio_set_level(BLINK_GPIO, 0); // LED on when state = 0 (active-LOW)
        vTaskDelay(pdMS_TO_TICKS(BLINK_PERIOD_MS));
        gpio_set_level(BLINK_GPIO, 1); // LED off when state = 1 (active-LOW)
        vTaskDelay(pdMS_TO_TICKS(BLINK_PERIOD_MS));
    }
}
