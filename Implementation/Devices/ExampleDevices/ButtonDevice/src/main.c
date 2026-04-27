#include "driver/gpio.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

#define BUTTON_GPIO GPIO_NUM_20
#define LED_GPIO GPIO_NUM_8

/* On ESP32-C3 SuperMini the built-in LED on GPIO8 is active-LOW:
   level 0 = LED on, level 1 = LED off. */

void app_main(void)
{
    gpio_reset_pin(BUTTON_GPIO);
    gpio_set_direction(BUTTON_GPIO, GPIO_MODE_INPUT);
    gpio_set_pull_mode(BUTTON_GPIO, GPIO_PULLDOWN_ONLY);

    gpio_reset_pin(LED_GPIO);
    gpio_set_direction(LED_GPIO, GPIO_MODE_OUTPUT);
    gpio_set_level(LED_GPIO, 1); /* off by default */

    while (1)
    {
        int pressed = gpio_get_level(BUTTON_GPIO);
        gpio_set_level(LED_GPIO, pressed ? 0 : 1); /* active-LOW: 0 = on */
        vTaskDelay(pdMS_TO_TICKS(10));
    }
}
