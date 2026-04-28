/* main.c -- ButtonDevice demo: swarm_connect captive-portal smoke test.
 *
 * Starts the onboarding Wi-Fi AP and captive portal, then logs the
 * CONNECTION_STRING every 10 seconds so you can verify the portal in a
 * browser while monitoring the serial output.
 *
 * Seeed XIAO ESP32C3 pin mapping:
 *   GPIO 8  -- built-in LED (active-HIGH)
 *   GPIO 20 -- button (not used in this smoke-test, reserved for later)
 */

#include "esp_log.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "swarm_library.h"

#define LED_GPIO 8
#define BUTTON_GPIO 20

#define TAG "button_main"

/* Slow-blink the LED while the portal is up so it is easy to see the device
 * is waiting for onboarding (2 s period = 1 s on / 1 s off). */
#define PORTAL_BLINK_PERIOD_MS 2000u

void app_main(void)
{
    /* Start the captive portal — creates the AP, DNS redirect, HTTP server. */
    ESP_ERROR_CHECK(swarm_connect_init());

    /* Blink the built-in LED to indicate the portal is active. */
    swarm_blink_init(LED_GPIO);
    swarm_blink_set_period_ms(PORTAL_BLINK_PERIOD_MS);
    swarm_blink_start();

    /* Periodically print the CONNECTION_STRING for serial monitor verification. */
    char cs_buf[80];
    while (1)
    {
        if (swarm_connect_get_connection_string(cs_buf, sizeof(cs_buf)) == ESP_OK)
        {
            ESP_LOGI(TAG, "=== CAPTIVE PORTAL ACTIVE ===");
            ESP_LOGI(TAG, "Connect to Wi-Fi: SwarmDevice (open)");
            ESP_LOGI(TAG, "Then open any URL in browser — you will be redirected.");
            ESP_LOGI(TAG, "CONNECTION_STRING: %s", cs_buf);
        }
        vTaskDelay(pdMS_TO_TICKS(10000));
    }
}
