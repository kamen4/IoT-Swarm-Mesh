/**
 * @file clock_ui.c
 * @brief Clock UI state-machine implementation.
 *
 * State transitions:
 *
 *   UI_MODE_CLOCK
 *     BTN_BOTH_HOLD  → UI_MODE_SET_H
 *     BTN_L_HOLD     → toggle sound_mute
 *     BTN_R_HOLD     → UI_MODE_SET_BRIGHT
 *     BTN_L_DCLICK   → UI_MODE_SET_QUIET_F
 *     BTN_R_DCLICK   → UI_MODE_SET_QUIET_T
 *
 *   UI_MODE_SET_H  ↔  UI_MODE_SET_M  (BTN_BOTH_CLICK toggles between them)
 *     BTN_BOTH_HOLD  → save time to RTC → UI_MODE_CLOCK
 *
 *   UI_MODE_SET_BRIGHT / SET_QUIET_F / SET_QUIET_T
 *     BTN_BOTH_CLICK or BOTH_HOLD → UI_MODE_CLOCK
 *
 *   Any setting mode → timeout after SET_TIMEOUT_MS of inactivity → UI_MODE_CLOCK
 *
 * Display conventions:
 *   - In SET_H the left two digits blink; right two show current minutes.
 *   - In SET_M the right two digits blink; left two show current hours.
 *   - In SET_BRIGHT / SET_QUIET_*: tm1637 shows the value; ring shows a bar graph.
 */
#include "clock_ui.h"
#include "tm1637.h"
#include "led_ring.h"
#include "buzzer.h"
#include "ds1302.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

/** Half-period of the digit blink animation in setting modes (ms) */
#define BLINK_MS 500
/** Auto-exit timeout for all setting modes (ms) */
#define SET_TIMEOUT_MS 10000
/** Auto-repeat first delay after hold starts (ms) */
#define REPEAT_FIRST_MS 600
/** Auto-repeat interval once repeating (ms) */
#define REPEAT_RATE_MS 120

/* TM1637 segment helpers */
#define SEG_OFF 0x00   /* all segments off — blank digit */
#define SEG_MINUS 0x40 /* middle segment only — minus sign */

static const uint8_t SEG_D[10] = {
    0x3F,
    0x06,
    0x5B,
    0x4F,
    0x66,
    0x6D,
    0x7D,
    0x07,
    0x7F,
    0x6F,
};

static ui_mode_t s_mode = UI_MODE_CLOCK;
static ui_settings_t s_settings = {0};

static uint8_t s_set_h = 0;
static uint8_t s_set_m = 0;

static bool s_blink_on = true;
static uint32_t s_blink_t = 0;
static uint32_t s_idle_t = 0;

/* Auto-repeat for hold in setting modes: dir=+1/-1, 0=inactive */
static int s_repeat_dir = 0;
static uint32_t s_repeat_next = 0;
static bool s_repeat_first = false;

static uint32_t now_ms(void)
{
    return (uint32_t)(xTaskGetTickCount() * portTICK_PERIOD_MS);
}

static void update_blink(uint32_t t)
{
    if (t - s_blink_t >= BLINK_MS)
    {
        s_blink_on = !s_blink_on;
        s_blink_t = t;
    }
}

static void show_edit_h(void)
{
    uint8_t d[4] = {
        s_blink_on ? SEG_D[s_set_h / 10] : SEG_OFF,
        s_blink_on ? (SEG_D[s_set_h % 10] | 0x80) : SEG_OFF,
        SEG_D[s_set_m / 10],
        SEG_D[s_set_m % 10],
    };
    tm1637_send_raw(d);
}

static void show_edit_m(void)
{
    uint8_t d[4] = {
        SEG_D[s_set_h / 10],
        SEG_D[s_set_h % 10] | 0x80,
        s_blink_on ? SEG_D[s_set_m / 10] : SEG_OFF,
        s_blink_on ? SEG_D[s_set_m % 10] : SEG_OFF,
    };
    tm1637_send_raw(d);
}

void clock_ui_init(uint8_t brightness, uint8_t quiet_from, uint8_t quiet_to)
{
    s_settings.brightness = brightness;
    s_settings.quiet_from = quiet_from;
    s_settings.quiet_to = quiet_to;
    s_settings.sound_mute = false;
    s_mode = UI_MODE_CLOCK;
    s_repeat_dir = 0;
}

const ui_settings_t *clock_ui_settings(void) { return &s_settings; }

/* ── helpers for autorepeat ──────────────────────────────────────────── */

/** Start autorepeat in the given direction; first tick after REPEAT_FIRST_MS. */
static void repeat_start(int dir, uint32_t t)
{
    s_repeat_dir = dir;
    s_repeat_next = t + REPEAT_FIRST_MS;
    s_repeat_first = true;
}

/** Stop autorepeat. */
static void repeat_stop(void) { s_repeat_dir = 0; }

/** Return true and schedule next tick if autorepeat fires this call. */
static bool repeat_tick(uint32_t t)
{
    if (s_repeat_dir == 0)
        return false;
    if ((int32_t)(t - s_repeat_next) < 0)
        return false;
    s_repeat_next = t + REPEAT_RATE_MS;
    return true;
}

/** Short high click played on each roulette spin step. */
static void roulette_tick(void) { buzzer_tone(1800, 12); }

/* Nokia 3310 ringtone -- 13 notes: frequency (Hz) and duration (ms).
 * Tempo = 180 BPM: 8th note = 110 ms, quarter = 235 ms, half = 485 ms
 * (note durations are shortened by 15 ms; vTaskDelay fills the gap). */
typedef struct
{
    uint32_t f;
    uint32_t d;
} ui_note_t;
static const ui_note_t s_nokia_tune[] = {
    {659, 110},
    {587, 110},
    {370, 235},
    {415, 235},
    {554, 110},
    {494, 110},
    {294, 235},
    {330, 235},
    {494, 110},
    {440, 110},
    {277, 235},
    {330, 235},
    {440, 485},
};
#define NOKIA_LEN ((int)(sizeof(s_nokia_tune) / sizeof(s_nokia_tune[0])))

ui_mode_t clock_ui_tick(uint8_t hour, uint8_t minute, btn_event_t ev)
{
    uint32_t t = now_ms();

    if (ev != BTN_NONE)
        s_idle_t = t;

    /* Auto-exit from any setting mode after inactivity */
    if (s_mode != UI_MODE_CLOCK && s_mode != UI_MODE_ROULETTE &&
        (t - s_idle_t) >= SET_TIMEOUT_MS)
    {
        s_mode = UI_MODE_CLOCK;
        repeat_stop();
        buzzer_beep_cancel();
    }

    if (s_mode != UI_MODE_CLOCK && s_mode != UI_MODE_ROULETTE)
        update_blink(t);

    switch (s_mode)
    {

    /* ════ Normal clock mode ════════════════════════════ */
    case UI_MODE_CLOCK:
        repeat_stop();
        switch (ev)
        {
        case BTN_BOTH_HOLD:
            s_set_h = hour;
            s_set_m = minute;
            s_mode = UI_MODE_SET_H;
            s_idle_t = t;
            buzzer_beep_short();
            break;
        case BTN_L_HOLD:
            s_settings.sound_mute = !s_settings.sound_mute;
            buzzer_beep_short();
            break;
        case BTN_R_HOLD:
            s_mode = UI_MODE_SET_BRIGHT;
            s_idle_t = t;
            buzzer_beep_short();
            break;
        case BTN_L_DCLICK:
            s_mode = UI_MODE_SET_QUIET_F;
            s_idle_t = t;
            buzzer_beep_short();
            break;
        case BTN_R_DCLICK:
            s_mode = UI_MODE_SET_QUIET_T;
            s_idle_t = t;
            buzzer_beep_short();
            break;
        case BTN_L_TCLICK:
            /* Triple-click left: launch roulette */
            s_mode = UI_MODE_ROULETTE;
            break;
        case BTN_BOTH_CLICK:
            if (s_settings.sound_mute)
                buzzer_beep_quarter();
            else
                buzzer_beep_short();
            break;
        default:
            break;
        }
        break;

    /* ════ Roulette animation (blocking) ════════════════ */
    case UI_MODE_ROULETTE:
    {
        uint8_t br = s_settings.brightness > 0 ? s_settings.brightness : 8;
        /* Spin + decelerate; returns true = red pocket, false = blue. */
        bool is_red = led_ring_roulette(br, roulette_tick);
        uint8_t wr = is_red ? br : 0;
        uint8_t wb = is_red ? 0 : br;
        /* Play Nokia Tune while blinking the ring in sync with each note.
         * Even-indexed notes: ring ON with winning colour.
         * Odd-indexed notes:  ring OFF (dark gap). */
        for (int i = 0; i < NOKIA_LEN; i++)
        {
            led_ring_set_all(i % 2 == 0 ? wr : 0, 0, i % 2 == 0 ? wb : 0);
            buzzer_tone(s_nokia_tune[i].f, s_nokia_tune[i].d);
            vTaskDelay(pdMS_TO_TICKS(15));
        }
        led_ring_set_all(0, 0, 0);
        s_mode = UI_MODE_CLOCK;
        break;
    }

    /* ════ Set HOURS ════════════════════════════════════ */
    case UI_MODE_SET_H:
        show_edit_h();
        led_ring_show_bar(s_set_h, 23, s_settings.brightness, 0, 0);
        switch (ev)
        {
        case BTN_R_CLICK:
            repeat_stop();
            s_set_h = (s_set_h + 1) % 24;
            buzzer_beep_short();
            break;
        case BTN_L_CLICK:
            repeat_stop();
            s_set_h = (s_set_h + 23) % 24;
            buzzer_beep_short();
            break;
        case BTN_R_HOLD:
            repeat_start(+1, t);
            s_set_h = (s_set_h + 1) % 24;
            break;
        case BTN_L_HOLD:
            repeat_start(-1, t);
            s_set_h = (s_set_h + 23) % 24;
            break;
        case BTN_BOTH_CLICK:
            repeat_stop();
            s_mode = UI_MODE_SET_M;
            buzzer_beep_short();
            break;
        case BTN_BOTH_HOLD:
            repeat_stop();
            ds1302_set_time(s_set_h, s_set_m, 0);
            s_mode = UI_MODE_CLOCK;
            buzzer_beep_confirm();
            break;
        default:
            if (ev == BTN_NONE && repeat_tick(t))
                s_set_h = (uint8_t)((s_set_h + 24 + s_repeat_dir) % 24);
            break;
        }
        break;

    /* ════ Set MINUTES ══════════════════════════════════ */
    case UI_MODE_SET_M:
        show_edit_m();
        led_ring_show_bar(s_set_m, 59, 0, s_settings.brightness, 0);
        switch (ev)
        {
        case BTN_R_CLICK:
            repeat_stop();
            s_set_m = (s_set_m + 1) % 60;
            buzzer_beep_short();
            break;
        case BTN_L_CLICK:
            repeat_stop();
            s_set_m = (s_set_m + 59) % 60;
            buzzer_beep_short();
            break;
        case BTN_R_HOLD:
            repeat_start(+1, t);
            s_set_m = (s_set_m + 1) % 60;
            break;
        case BTN_L_HOLD:
            repeat_start(-1, t);
            s_set_m = (s_set_m + 59) % 60;
            break;
        case BTN_BOTH_CLICK:
            repeat_stop();
            s_mode = UI_MODE_SET_H;
            buzzer_beep_short();
            break;
        case BTN_BOTH_HOLD:
            repeat_stop();
            ds1302_set_time(s_set_h, s_set_m, 0);
            s_mode = UI_MODE_CLOCK;
            buzzer_beep_confirm();
            break;
        default:
            if (ev == BTN_NONE && repeat_tick(t))
                s_set_m = (uint8_t)((s_set_m + 60 + s_repeat_dir) % 60);
            break;
        }
        break;

    /* ════ Set BRIGHTNESS ═══════════════════════════════ */
    case UI_MODE_SET_BRIGHT:
    {
        uint8_t br = s_settings.brightness;
        tm1637_show_right(br);
        led_ring_show_bar(br, 64, 0, 0, br > 0 ? br : 1);
        switch (ev)
        {
        case BTN_R_CLICK:
            repeat_stop();
            if (br < 64)
            {
                s_settings.brightness++;
                buzzer_beep_short();
            }
            break;
        case BTN_L_CLICK:
            repeat_stop();
            if (br > 1)
            {
                s_settings.brightness--;
                buzzer_beep_short();
            }
            break;
        case BTN_R_HOLD:
            repeat_start(+1, t);
            if (br < 64)
                s_settings.brightness++;
            break;
        case BTN_L_HOLD:
            repeat_start(-1, t);
            if (br > 1)
                s_settings.brightness--;
            break;
        case BTN_R_DCLICK:
            repeat_stop();
            s_settings.brightness = 64;
            buzzer_beep_short();
            break;
        case BTN_L_DCLICK:
            repeat_stop();
            s_settings.brightness = 1;
            buzzer_beep_short();
            break;
        case BTN_BOTH_CLICK:
        case BTN_BOTH_HOLD:
            repeat_stop();
            s_mode = UI_MODE_CLOCK;
            buzzer_beep_confirm();
            break;
        default:
            if (ev == BTN_NONE && repeat_tick(t))
            {
                uint8_t b = s_settings.brightness;
                if (s_repeat_dir > 0 && b < 64)
                    s_settings.brightness++;
                if (s_repeat_dir < 0 && b > 1)
                    s_settings.brightness--;
            }
            break;
        }
        break;
    }

    /* ════ Set quiet-time START ═════════════════════════ */
    case UI_MODE_SET_QUIET_F:
        tm1637_show_left(s_settings.quiet_from);
        led_ring_show_bar(s_settings.quiet_from, 23,
                          s_settings.brightness / 2, s_settings.brightness / 4, 0);
        switch (ev)
        {
        case BTN_R_CLICK:
            repeat_stop();
            s_settings.quiet_from = (s_settings.quiet_from + 1) % 24;
            buzzer_beep_short();
            break;
        case BTN_L_CLICK:
            repeat_stop();
            s_settings.quiet_from = (s_settings.quiet_from + 23) % 24;
            buzzer_beep_short();
            break;
        case BTN_R_HOLD:
            repeat_start(+1, t);
            s_settings.quiet_from = (s_settings.quiet_from + 1) % 24;
            break;
        case BTN_L_HOLD:
            repeat_start(-1, t);
            s_settings.quiet_from = (s_settings.quiet_from + 23) % 24;
            break;
        case BTN_BOTH_CLICK:
        case BTN_BOTH_HOLD:
            repeat_stop();
            s_mode = UI_MODE_CLOCK;
            buzzer_beep_confirm();
            break;
        default:
            if (ev == BTN_NONE && repeat_tick(t))
                s_settings.quiet_from = (uint8_t)((s_settings.quiet_from + 24 + s_repeat_dir) % 24);
            break;
        }
        break;

    /* ════ Set quiet-time END ═══════════════════════════ */
    case UI_MODE_SET_QUIET_T:
        tm1637_show_right(s_settings.quiet_to);
        led_ring_show_bar(s_settings.quiet_to, 23,
                          s_settings.brightness / 2, s_settings.brightness / 4, 0);
        switch (ev)
        {
        case BTN_R_CLICK:
            repeat_stop();
            s_settings.quiet_to = (s_settings.quiet_to + 1) % 24;
            buzzer_beep_short();
            break;
        case BTN_L_CLICK:
            repeat_stop();
            s_settings.quiet_to = (s_settings.quiet_to + 23) % 24;
            buzzer_beep_short();
            break;
        case BTN_R_HOLD:
            repeat_start(+1, t);
            s_settings.quiet_to = (s_settings.quiet_to + 1) % 24;
            break;
        case BTN_L_HOLD:
            repeat_start(-1, t);
            s_settings.quiet_to = (s_settings.quiet_to + 23) % 24;
            break;
        case BTN_BOTH_CLICK:
        case BTN_BOTH_HOLD:
            repeat_stop();
            s_mode = UI_MODE_CLOCK;
            buzzer_beep_confirm();
            break;
        default:
            if (ev == BTN_NONE && repeat_tick(t))
                s_settings.quiet_to = (uint8_t)((s_settings.quiet_to + 24 + s_repeat_dir) % 24);
            break;
        }
        break;
    }

    return s_mode;
}
