/* swarm_blink.c -- blink module implementation.
 * Uses a FreeRTOS task to toggle a GPIO at a configurable period.
 * Half the period the pin is high, half the period the pin is low. */

#include "swarm_blink.h"

#include "driver/gpio.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

static int s_gpio_num = -1;
static uint32_t s_period_ms = 500;
static TaskHandle_t s_task_handle = NULL;

static void blink_task(void *arg)
{
    while (1)
    {
        gpio_set_level((gpio_num_t)s_gpio_num, 1);
        vTaskDelay(pdMS_TO_TICKS(s_period_ms / 2));
        gpio_set_level((gpio_num_t)s_gpio_num, 0);
        vTaskDelay(pdMS_TO_TICKS(s_period_ms / 2));
    }
}

void swarm_blink_init(int gpio_num)
{
    s_gpio_num = gpio_num;
    gpio_reset_pin((gpio_num_t)gpio_num);
    gpio_set_direction((gpio_num_t)gpio_num, GPIO_MODE_OUTPUT);
    gpio_set_level((gpio_num_t)gpio_num, 0);
}

void swarm_blink_set_period_ms(uint32_t period_ms)
{
    /* Guard against a period so short that half-periods round to 0 ticks. */
    s_period_ms = (period_ms < 2) ? 2 : period_ms;
}

void swarm_blink_start(void)
{
    if (s_task_handle == NULL && s_gpio_num >= 0)
    {
        xTaskCreate(blink_task, "swarm_blink", 2048, NULL, 5, &s_task_handle);
    }
}

void swarm_blink_stop(void)
{
    if (s_task_handle != NULL)
    {
        vTaskDelete(s_task_handle);
        s_task_handle = NULL;
        gpio_set_level((gpio_num_t)s_gpio_num, 0);
    }
}
