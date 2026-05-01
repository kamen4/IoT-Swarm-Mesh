/**
 * @file led_ring.c
 * @brief WS2812B ring driver — wraps the espressif/led_strip IDF component.
 *
 * Coordinate system:
 *   Physical LED index 0 is at the bottom of the ring (6 o'clock).
 *   s_offset = 6 shifts the origin so that index 0 in clock-space maps
 *   to the physical LED at 12 o'clock.
 */
#include "led_ring.h"
#include "led_strip.h"
#include "esp_log.h"
#include "esp_random.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

/* Module state — set once by led_ring_init() */
static led_strip_handle_t s_strip;
static int s_count = 12; /* total number of LEDs */
static int s_offset = 6; /* physical index of the 12 o'clock position */

void led_ring_init(int led_gpio, int count, int offset)
{
    s_count = count;
    s_offset = offset;

    led_strip_config_t strip_cfg = {
        .strip_gpio_num = led_gpio,
        .max_leds = count,
        .color_component_format = LED_STRIP_COLOR_COMPONENT_FMT_GRB,
    };
    led_strip_rmt_config_t rmt_cfg = {.resolution_hz = 10 * 1000 * 1000};
    ESP_ERROR_CHECK(led_strip_new_rmt_device(&strip_cfg, &rmt_cfg, &s_strip));
    led_strip_clear(s_strip);
}

void led_ring_show_clock(uint8_t hour, uint8_t minute, uint8_t brightness)
{
    /* Hour arc: lit from 12-o'clock (s_offset) up to and including h_steps pixels.
     * Minute hand is a single green pixel (one dot). */
    uint8_t h_steps = hour % 12; /* 0..11 steps from 12 o'clock */
    uint8_t m_pos = (minute / 5 + s_offset) % s_count;

    for (int i = 0; i < s_count; i++)
    {
        /* Determine if pixel i is inside the hour arc.
         * Arc starts at s_offset and spans h_steps pixels clockwise. */
        int rel = (i - s_offset + s_count) % s_count; /* 0 = 12 o'clock */
        bool in_arc = (h_steps == 0) ? (rel == 0) : (rel <= h_steps);

        uint8_t r = in_arc ? brightness : 0;
        uint8_t g = (i == m_pos) ? brightness : 0;
        /* Overlapping pixel (arc tip == minute) shows yellow */
        led_strip_set_pixel(s_strip, i, r, g, 0);
    }
    led_strip_refresh(s_strip);
}

void led_ring_show_bar(uint8_t val, uint8_t max_val,
                       uint8_t r, uint8_t g, uint8_t b)
{
    uint8_t lit = (uint8_t)((val * s_count + max_val / 2) / max_val);
    for (int i = 0; i < s_count; i++)
    {
        if (i < lit)
            led_strip_set_pixel(s_strip, i, r, g, b);
        else
            led_strip_set_pixel(s_strip, i, 0, 0, 0);
    }
    led_strip_refresh(s_strip);
}

void led_ring_set_all(uint8_t r, uint8_t g, uint8_t b)
{
    for (int i = 0; i < s_count; i++)
        led_strip_set_pixel(s_strip, i, r, g, b);
    led_strip_refresh(s_strip);
}

/** Light a single pixel at position pos with its roulette colour.
 *  Odd positions = red (casino red), even positions = blue (casino black). */
static void roulette_show_dot(int pos, uint8_t brightness)
{
    uint8_t r = (pos % 2 != 0) ? brightness : 0;
    uint8_t b = (pos % 2 == 0) ? brightness : 0;
    for (int i = 0; i < s_count; i++)
        led_strip_set_pixel(s_strip, i, 0, 0, 0);
    led_strip_set_pixel(s_strip, pos, r, 0, b);
    led_strip_refresh(s_strip);
}

bool led_ring_roulette(uint8_t brightness, void (*tick_cb)(void))
{
    /* Mix hardware RNG with tick counter so consecutive calls differ even
     * when the hardware RNG has not accumulated much entropy yet. */
    uint32_t seed = esp_random() ^ ((uint32_t)xTaskGetTickCount() * 2654435761UL);

    /* Random extra steps in spin phase so pos after spin is unpredictable */
    int rand_extra = (int)(seed % (uint32_t)s_count);
    /* Shift seed bits to get an independent target value */
    int target = (int)((seed >> 7) % (uint32_t)s_count);

    /* Winning colour: odd index = red, even = blue */
    bool is_red = (target % 2 != 0);

    /* Spin phase: 3 full turns + rand_extra extra steps.
     * Delay per step interpolates 40 ms (fast) -> 150 ms (slow). */
    int total_spin = s_count * 3 + rand_extra;
    int pos = 0;
    for (int step = 0; step < total_spin; step++)
    {
        int delay_ms = 40 + (110 * step) / total_spin;
        roulette_show_dot(pos, brightness);
        if (tick_cb)
            tick_cb();
        vTaskDelay(pdMS_TO_TICKS(delay_ms));
        pos = (pos + 1) % s_count;
    }

    /* Deceleration phase: advance from current pos until we reach target */
    int extra = (target - pos + s_count) % s_count;
    if (extra == 0)
        extra = s_count; /* ensure at least one more full lap */
    for (int step = 0; step < extra; step++)
    {
        int delay_ms = 150 + (150 * step) / (extra > 1 ? extra : 1);
        roulette_show_dot(pos, brightness);
        if (tick_cb)
            tick_cb();
        vTaskDelay(pdMS_TO_TICKS(delay_ms));
        pos = (pos + 1) % s_count;
    }

    /* Show winning pixel; caller handles sound and blink effects */
    roulette_show_dot(target, brightness);
    return is_red;
}
