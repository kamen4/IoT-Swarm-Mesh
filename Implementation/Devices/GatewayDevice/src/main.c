#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/usb_serial_jtag.h"
#include "esp_log.h"

#define BUF_SIZE 256

static void echo_task(void *arg)
{
    uint8_t rx[BUF_SIZE];
    uint8_t tx[BUF_SIZE + 2];

    while (1)
    {
        int len = usb_serial_jtag_read_bytes(rx, sizeof(rx) - 1, pdMS_TO_TICKS(20));
        if (len > 0)
        {
            tx[0] = 'E';
            memcpy(tx + 1, rx, len);
            tx[len + 1] = 'E';
            usb_serial_jtag_write_bytes(tx, len + 2, pdMS_TO_TICKS(100));
        }
    }
}

void app_main(void)
{
    esp_log_level_set("*", ESP_LOG_NONE);

    const usb_serial_jtag_driver_config_t jtag_cfg = {
        .rx_buffer_size = BUF_SIZE,
        .tx_buffer_size = BUF_SIZE,
    };
    usb_serial_jtag_driver_install(&jtag_cfg);

    xTaskCreate(echo_task, "echo_task", 2048, NULL, tskIDLE_PRIORITY + 1, NULL);
}
