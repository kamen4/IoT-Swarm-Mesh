/**
 * @file buttons.c
 * @brief Two-button state-machine — non-blocking, poll-based.
 *
 * Algorithm overview:
 *   1. If both buttons are pressed simultaneously they are handled as a
 *      separate BOTH_CLICK / BOTH_HOLD pair; individual button state is
 *      ignored while both are down.
 *   2. For each individual button:
 *      - Rising edge  → record press_time.
 *      - During press → if held >= HOLD_MS fire HOLD immediately.
 *      - Falling edge → if not held, increment click_cnt and record release time.
 *   3. After a release, if DCLICK_MS elapses with no new press, emit the
 *      appropriate CLICK / DCLICK / TCLICK event based on click_cnt.
 */
#include "buttons.h"
#include "driver/gpio.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

/** Minimum hold duration to generate a HOLD event (ms) */
#define HOLD_MS 700
/** Maximum gap between clicks for multi-click detection (ms) */
#define DCLICK_MS 350

static int s_gpio_l;
static int s_gpio_r;

typedef struct
{
    int gpio;
    bool pressed;
    bool held;
    uint32_t press_time;
    uint8_t click_cnt;
    uint32_t last_release;
} btn_state_t;

/* Per-button tracking state (one instance per button) */
static btn_state_t s_bl; /* left  button */
static btn_state_t s_br; /* right button */

/** Return the current time in milliseconds (wraps every ~49 days). */
static uint32_t now_ms(void)
{
    return (uint32_t)(xTaskGetTickCount() * portTICK_PERIOD_MS);
}

void buttons_init(int gpio_l, int gpio_r)
{
    s_gpio_l = gpio_l;
    s_gpio_r = gpio_r;

    gpio_config_t cfg = {
        .pin_bit_mask = (1ULL << gpio_l) | (1ULL << gpio_r),
        .mode = GPIO_MODE_INPUT,
        .pull_up_en = GPIO_PULLUP_ENABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type = GPIO_INTR_DISABLE,
    };
    gpio_config(&cfg);

    s_bl.gpio = gpio_l;
    s_br.gpio = gpio_r;
}

btn_event_t buttons_poll(void)
{
    uint32_t t = now_ms();
    bool lv = (gpio_get_level(s_gpio_l) == 0);
    bool rv = (gpio_get_level(s_gpio_r) == 0);

    /* ── обе кнопки одновременно ── */
    static bool both_held = false;
    static uint32_t both_press_t = 0;
    static bool both_fired = false;

    if (lv && rv)
    {
        if (!both_held)
        {
            both_held = true;
            both_press_t = t;
            both_fired = false;
        }
        else if (!both_fired && (t - both_press_t) >= HOLD_MS)
        {
            both_fired = true;
            return BTN_BOTH_HOLD;
        }
        return BTN_NONE;
    }
    else if (both_held)
    {
        both_held = false;
        if (!both_fired)
        {
            both_fired = true;
            return BTN_BOTH_CLICK;
        }
    }

    /* ── обработка одиночных кнопок ── */
    btn_event_t ev = BTN_NONE;

    for (int i = 0; i < 2; i++)
    {
        btn_state_t *b = (i == 0) ? &s_bl : &s_br;
        bool v = (i == 0) ? lv : rv;

        if (v && !b->pressed)
        {
            b->pressed = true;
            b->held = false;
            b->press_time = t;
        }
        else if (v && b->pressed && !b->held)
        {
            if ((t - b->press_time) >= HOLD_MS)
            {
                b->held = true;
                b->click_cnt = 0;
                ev = (i == 0) ? BTN_L_HOLD : BTN_R_HOLD;
            }
        }
        else if (!v && b->pressed)
        {
            b->pressed = false;
            if (!b->held)
            {
                uint32_t since = t - b->last_release;
                if (b->click_cnt > 0 && since > DCLICK_MS)
                    b->click_cnt = 0;
                b->click_cnt++;
                b->last_release = t;
            }
            b->held = false;
        }

        if (!v && !b->pressed && b->click_cnt > 0)
        {
            if ((t - b->last_release) >= DCLICK_MS)
            {
                uint8_t cnt = b->click_cnt;
                b->click_cnt = 0;
                if (cnt == 1)
                    ev = (i == 0) ? BTN_L_CLICK : BTN_R_CLICK;
                else if (cnt == 2)
                    ev = (i == 0) ? BTN_L_DCLICK : BTN_R_DCLICK;
                else
                    ev = (i == 0) ? BTN_L_TCLICK : BTN_R_TCLICK;
            }
        }
    }
    return ev;
}
