/**
 * @file main.c
 * @brief ClockDevice -- entry point and main event loop.
 *
 * Hardware summary:
 *   - DS1302 RTC      : keeps time with a CR2032 backup battery (VBAT pin)
 *   - TM1637 display  : shows HH:MM (blinks digits in settings modes)
 *   - Passive buzzer  : hourly melody, quarter-hour beeps, UI feedback
 *   - WS2812B ring    : 12-LED ring -- red arc (hour), green dot (minute)
 *   - 2 x push buttons: navigate and change settings
 *
 * Time persistence strategy:
 *   The DS1302 keeps time from its own CR2032 battery (VBAT/VCC2 pin).
 *   If that battery is absent or dead the RTC loses time on every power cut.
 *   As a software fallback, the last known H:M is saved to NVS flash every
 *   minute.  On boot, if ds1302_init() reports the oscillator was halted
 *   (CH bit set), the NVS snapshot is restored to the DS1302 so the clock
 *   shows the last saved minute rather than resetting to 12:00.
 *   NOTE: this does NOT make the clock accurate during power-off; connect
 *   a CR2032 to DS1302 VBAT for proper timekeeping when the MCU is off.
 *
 * Architecture:
 *   All hardware-specific code lives in lib/.  This file only defines GPIO
 *   pin assignments, calls the init functions, and runs the main loop.
 *
 * Main loop (every UPDATE_MS = 50 ms):
 *   1. Every 1 second  -- read RTC, update display and ring, fire sounds.
 *   2. Every iteration -- poll buttons, feed events into clock_ui_tick().
 */
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "nvs_flash.h"
#include "nvs.h"
#include <stdbool.h>

#include "ds1302.h"
#include "tm1637.h"
#include "buzzer.h"
#include "led_ring.h"
#include "buttons.h"
#include "clock_ui.h"

/* ── GPIO pin assignments ─────────────────────────────── */
#define TM_CLK_GPIO   0   /* TM1637 clock */
#define TM_DIO_GPIO   1   /* TM1637 data  */
#define BUZZER_GPIO   3   /* Passive buzzer */
#define BTN_R_GPIO    5   /* Right button (to GND) */
#define BTN_L_GPIO    6   /* Left  button (to GND) */
#define DS_CLK_GPIO   9   /* DS1302 clock */
#define DS_DAT_GPIO   10  /* DS1302 data  */
#define DS_RST_GPIO   20  /* DS1302 RST/CE — requires 10 kΩ pull-down to GND on the wire */
#define LED_GPIO      21  /* WS2812B DI */

/* ── Clock / UI configuration ────────────────────────── */
#define LED_COUNT    12   /* Number of LEDs in the ring */
#define LED_OFFSET   6    /* Physical index of the 12-o'clock LED */
#define LED_BRIGHT   8    /* Default ring brightness (1-255) */
#define QUIET_FROM   22   /* Start of quiet period (hour) */
#define QUIET_TO     6    /* End   of quiet period (hour) */
#define UPDATE_MS    50   /* Main loop period in milliseconds */

/* ── NVS helpers (software time backup to flash) ────── */

/** Save hour and minute to NVS so they survive a power cut. */
static void clock_nvs_save(uint8_t h, uint8_t m)
{
    nvs_handle_t hdl;
    if (nvs_open("clock", NVS_READWRITE, &hdl) == ESP_OK)
    {
        nvs_set_u8(hdl, "h", h);
        nvs_set_u8(hdl, "m", m);
        nvs_commit(hdl);
        nvs_close(hdl);
    }
}

/** Load hour and minute from NVS.  Returns true if values were found. */
static bool clock_nvs_load(uint8_t *h, uint8_t *m)
{
    nvs_handle_t hdl;
    if (nvs_open("clock", NVS_READONLY, &hdl) != ESP_OK)
        return false;
    esp_err_t e1 = nvs_get_u8(hdl, "h", h);
    esp_err_t e2 = nvs_get_u8(hdl, "m", m);
    nvs_close(hdl);
    return (e1 == ESP_OK && e2 == ESP_OK);
}

void app_main(void)
{
    /* NVS must be ready before ds1302_init so we can restore saved time */
    esp_err_t nvs_ret = nvs_flash_init();
    if (nvs_ret == ESP_ERR_NVS_NO_FREE_PAGES ||
        nvs_ret == ESP_ERR_NVS_NEW_VERSION_FOUND)
    {
        /* NVS partition was truncated; erase and reinitialise */
        nvs_flash_erase();
        nvs_flash_init();
    }
    /* true = RTC oscillator was halted (battery missing/dead); time was reset */
    bool rtc_fresh = ds1302_init(DS_RST_GPIO, DS_DAT_GPIO, DS_CLK_GPIO);
    tm1637_init(TM_CLK_GPIO, TM_DIO_GPIO);
    buzzer_init(BUZZER_GPIO);
    led_ring_init(LED_GPIO, LED_COUNT, LED_OFFSET);
    buttons_init(BTN_L_GPIO, BTN_R_GPIO);
    clock_ui_init(LED_BRIGHT, QUIET_FROM, QUIET_TO);

    /* Restore last saved time if RTC lost power */
    if (rtc_fresh)
    {
        uint8_t sh = 12, sm = 0;
        if (clock_nvs_load(&sh, &sm))
            ds1302_set_time(sh, sm, 0);
    }

    buzzer_beep_quarter(); /* сигнал включения */

    uint8_t hour = 0, minute = 0, second = 0;
    uint8_t last_min = 0xFF;
    uint32_t last_sec_ms = 0;

    while (1)
    {
        uint32_t t = (uint32_t)(xTaskGetTickCount() * portTICK_PERIOD_MS);

        /* Читаем RTC раз в секунду */
        if (t - last_sec_ms >= 1000)
        {
            last_sec_ms = t;
            ds1302_get_time(&hour, &minute, &second);

            const ui_settings_t *s = clock_ui_settings();

            /* Обновляем дисплей и кольцо только в нормальном режиме */
            if (clock_ui_tick(hour, minute, BTN_NONE) == UI_MODE_CLOCK)
            {
                tm1637_show_time(hour, minute);
                led_ring_show_clock(hour, minute, s->brightness);

                /* Звуковые сигналы по расписанию */
                if (minute != last_min && !s->sound_mute)
                {
                    bool quiet = (hour >= s->quiet_from || hour < s->quiet_to);
                    if (!quiet)
                    {
                        if (minute == 0)
                            buzzer_play_hour_melody();
                        else if (minute % 15 == 0)
                            buzzer_beep_quarter();
                    }
                }
                /* Save current time to NVS every minute as a power-loss backup */
                if (minute != last_min)
                    clock_nvs_save(hour, minute);
                last_min = minute;
            }
        }

        /* Опрос кнопок и обновление UI */
        btn_event_t ev = buttons_poll();
        const ui_settings_t *s = clock_ui_settings();
        ui_mode_t mode = clock_ui_tick(hour, minute, ev);

        /* В режиме настройки кольцо управляется изнутри clock_ui,
         * в нормальном — обновляем здесь каждый тик */
        if (mode == UI_MODE_CLOCK && ev != BTN_NONE)
        {
            led_ring_show_clock(hour, minute, s->brightness);
        }

        vTaskDelay(pdMS_TO_TICKS(UPDATE_MS));
    }
}
