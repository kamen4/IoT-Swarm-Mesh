#include <stdio.h>
#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/usb_serial_jtag.h"
#include "driver/gpio.h"

#define BUF_SIZE 256
#define PIN_TOGGLE_CMD "PIN_TOGGLE:8"
#define PIN_STATE_FMT "PIN_STATE:8:%d\n"

static int s_pin_state = 0;

static void gateway_task(void *arg)
{
    /* Buffers are static to avoid placing 768 bytes on the task stack.
     * gateway_task is the only user of these variables (single task, no re-entrancy). */
    static uint8_t rx[BUF_SIZE];
    static char line[BUF_SIZE];
    static char resp[BUF_SIZE];
    int line_len = 0;

    while (1)
    {
        int len = usb_serial_jtag_read_bytes(rx, sizeof(rx) - 1, pdMS_TO_TICKS(20));
        for (int i = 0; i < len; i++)
        {
            uint8_t c = rx[i];

            if (c == '\r')
            {
                continue;
            }

            if (c == '\n')
            {
                line[line_len] = '\0';

                if (strcmp(line, PIN_TOGGLE_CMD) == 0)
                {
                    s_pin_state = s_pin_state ? 0 : 1;
                    gpio_set_level(GPIO_NUM_8, s_pin_state);

                    int resp_len = snprintf(resp, sizeof(resp), PIN_STATE_FMT, gpio_get_level(GPIO_NUM_8));
                    usb_serial_jtag_write_bytes((uint8_t *)resp, resp_len, pdMS_TO_TICKS(100));
                }

                line_len = 0;
            }
            else if (line_len < (BUF_SIZE - 1))
            {
                line[line_len++] = (char)c;
            }
        }
    }
}

void app_main(void)
{
    const usb_serial_jtag_driver_config_t jtag_cfg = {
        .rx_buffer_size = 1024,
        .tx_buffer_size = 1024,
    };
    usb_serial_jtag_driver_install(&jtag_cfg);

    gpio_reset_pin(GPIO_NUM_8);
    gpio_set_direction(GPIO_NUM_8, GPIO_MODE_OUTPUT);
    gpio_set_level(GPIO_NUM_8, 0);

    /* Signal to the host that the device has booted and is ready to receive commands. */
    const uint8_t ready_msg[] = "READY\n";
    usb_serial_jtag_write_bytes(ready_msg, sizeof(ready_msg) - 1, pdMS_TO_TICKS(100));

    xTaskCreate(gateway_task, "gateway_task", 4096, NULL, tskIDLE_PRIORITY + 1, NULL);
}
