/**
 * @file tm1637.c
 * @brief TM1637 display driver — bit-banged two-wire protocol.
 *
 * Protocol summary:
 *   1. Start  : DIO falls while CLK is high.
 *   2. Data   : bytes sent LSB-first; DIO is sampled on the rising CLK edge.
 *   3. ACK    : after each byte master releases DIO; device pulls it low.
 *   4. Stop   : DIO rises while CLK is high.
 *
 * Each display update requires three transactions:
 *   a) 0x40 — data command (auto-increment address mode)
 *   b) 0xC0 + 4 data bytes — write to address 0x00..0x03
 *   c) 0x8F — display-on command (brightness = max)
 */
#include "tm1637.h"
#include "driver/gpio.h"
#include "rom/ets_sys.h"

/* Segment code for the minus sign '-' */
#define SEG_MINUS 0x40
/* Segment code for a blank/off digit */
#define SEG_OFF 0x00

static const uint8_t SEG_DIGITS[10] = {
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

/* Module-level GPIO numbers — set once by tm1637_init() */
static int s_clk;
static int s_dio;
/* Bit-bang delay in microseconds — 5 µs gives ~100 kHz effective clock */
static int s_delay = 5;

static inline void clk_hi(void)
{
    gpio_set_level(s_clk, 1);
    ets_delay_us(s_delay);
}
static inline void clk_lo(void)
{
    gpio_set_level(s_clk, 0);
    ets_delay_us(s_delay);
}
static inline void dio_hi(void)
{
    gpio_set_level(s_dio, 1);
    ets_delay_us(s_delay);
}
static inline void dio_lo(void)
{
    gpio_set_level(s_dio, 0);
    ets_delay_us(s_delay);
}

/* Start condition: DIO falls while CLK is high */
static void tm_start(void)
{
    dio_hi();
    clk_hi();
    dio_lo();
    clk_lo();
}
/* Stop condition: DIO rises while CLK is high */
static void tm_stop(void)
{
    clk_lo();
    dio_lo();
    clk_hi();
    dio_hi();
}

/* Transmit one byte LSB-first; generate one CLK pulse for ACK at the end */
static void tm_write_byte(uint8_t b)
{
    for (int i = 0; i < 8; i++)
    {
        clk_lo();
        if (b & 0x01)
            dio_hi();
        else
            dio_lo();
        b >>= 1;
        clk_hi();
    }
    /* ACK clock pulse — master releases DIO, device pulls it low */
    clk_lo();
    dio_hi();
    clk_hi();
    clk_lo();
}

void tm1637_init(int clk_gpio, int dio_gpio)
{
    s_clk = clk_gpio;
    s_dio = dio_gpio;
    gpio_config_t cfg = {
        .pin_bit_mask = (1ULL << clk_gpio) | (1ULL << dio_gpio),
        .mode = GPIO_MODE_OUTPUT_OD,
        .pull_up_en = GPIO_PULLUP_ENABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type = GPIO_INTR_DISABLE,
    };
    gpio_config(&cfg);
}

void tm1637_send_raw(uint8_t seg[4])
{
    tm_start();
    tm_write_byte(0x40);
    tm_stop();
    tm_start();
    tm_write_byte(0xC0);
    for (int i = 0; i < 4; i++)
        tm_write_byte(seg[i]);
    tm_stop();
    tm_start();
    tm_write_byte(0x8F);
    tm_stop();
}

void tm1637_show_time(uint8_t h, uint8_t m)
{
    uint8_t d[4] = {
        SEG_DIGITS[h / 10],
        SEG_DIGITS[h % 10] | 0x80,
        SEG_DIGITS[m / 10],
        SEG_DIGITS[m % 10],
    };
    tm1637_send_raw(d);
}

void tm1637_show_right(uint8_t val)
{
    uint8_t d[4] = {SEG_OFF, SEG_MINUS, SEG_DIGITS[val / 10], SEG_DIGITS[val % 10]};
    tm1637_send_raw(d);
}

void tm1637_show_left(uint8_t val)
{
    uint8_t d[4] = {SEG_DIGITS[val / 10], SEG_DIGITS[val % 10] | 0x80, SEG_MINUS, SEG_OFF};
    tm1637_send_raw(d);
}
