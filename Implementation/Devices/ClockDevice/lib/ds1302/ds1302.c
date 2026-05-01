/**
 * @file ds1302.c
 * @brief DS1302 RTC driver — bit-banged 3-wire serial protocol.
 *
 * Protocol summary:
 *   - RST (CE) must be HIGH for the entire transaction.
 *   - Command byte is written first (LSB-first), then data is written or read.
 *   - Bit 0 of the command byte selects R(1) / W(0).
 *   - Write-protect register (0x8E): bit 7 = 1 locks all registers.
 *
 * Register map used:
 *   0x80 / 0x81  Seconds  (bit 7 = CH — clock halt flag)
 *   0x82 / 0x83  Minutes
 *   0x84 / 0x85  Hours    (bit 7 = 12/24 mode; bit 6 = 24h mode when 0)
 *   0x8E / 0x8F  Write-protect
 */
#include "ds1302.h"
#include "driver/gpio.h"
#include "rom/ets_sys.h"

/* Module-level GPIO numbers — set once by ds1302_init() */
static int s_rst;
static int s_dat;
static int s_clk;

/* BCD ↔ decimal conversion helpers */
static uint8_t bcd2dec(uint8_t b) { return (b >> 4) * 10 + (b & 0x0F); }
static uint8_t dec2bcd(uint8_t d) { return ((d / 10) << 4) | (d % 10); }

/* Write one byte LSB-first; DAT is driven as output */
static void ds_write_byte(uint8_t b)
{
    gpio_set_direction(s_dat, GPIO_MODE_OUTPUT);
    for (int i = 0; i < 8; i++)
    {
        gpio_set_level(s_dat, b & 0x01);
        b >>= 1;
        ets_delay_us(2);
        gpio_set_level(s_clk, 1);
        ets_delay_us(2);
        gpio_set_level(s_clk, 0);
        ets_delay_us(2);
    }
}

/* Read one byte LSB-first; DAT is switched to input */
static uint8_t ds_read_byte(void)
{
    gpio_set_direction(s_dat, GPIO_MODE_INPUT);
    uint8_t b = 0;
    for (int i = 0; i < 8; i++)
    {
        if (gpio_get_level(s_dat))
            b |= (1 << i);
        gpio_set_level(s_clk, 1);
        ets_delay_us(2);
        gpio_set_level(s_clk, 0);
        ets_delay_us(2);
    }
    return b;
}

static uint8_t ds_read_reg(uint8_t cmd)
{
    if (s_rst >= 0)
    {
        gpio_set_level(s_rst, 1);
        ets_delay_us(4);
    }
    ds_write_byte(cmd);
    uint8_t v = ds_read_byte();
    if (s_rst >= 0)
    {
        gpio_set_level(s_rst, 0);
        ets_delay_us(4);
    }
    return v;
}

static void ds_write_reg(uint8_t cmd, uint8_t val)
{
    if (s_rst >= 0)
    {
        gpio_set_level(s_rst, 1);
        ets_delay_us(4);
    }
    ds_write_byte(cmd);
    ds_write_byte(val);
    if (s_rst >= 0)
    {
        gpio_set_level(s_rst, 0);
        ets_delay_us(4);
    }
}

bool ds1302_init(int rst_gpio, int dat_gpio, int clk_gpio)
{
    s_rst = rst_gpio;
    s_dat = dat_gpio;
    s_clk = clk_gpio;

    /* Configure CLK as output and drive LOW immediately.
     * Configure RST as output and drive LOW only if a GPIO number is given.
     * If rst_gpio < 0, RST is hardware-tied HIGH (pull-up to VCC); the CLK
     * pull-down resistor prevents any spurious transactions during boot. */
    gpio_config_t cfg_clk = {
        .pin_bit_mask = (1ULL << clk_gpio),
        .mode = GPIO_MODE_OUTPUT,
        .pull_up_en = GPIO_PULLUP_DISABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type = GPIO_INTR_DISABLE,
    };
    gpio_config(&cfg_clk);
    gpio_set_level(s_clk, 0);

    if (rst_gpio >= 0)
    {
        gpio_config_t cfg_rst = {
            .pin_bit_mask = (1ULL << rst_gpio),
            .mode = GPIO_MODE_OUTPUT,
            .pull_up_en = GPIO_PULLUP_DISABLE,
            .pull_down_en = GPIO_PULLDOWN_DISABLE,
            .intr_type = GPIO_INTR_DISABLE,
        };
        gpio_config(&cfg_rst);
        gpio_set_level(s_rst, 0);
    }

    gpio_config_t cfg_in = {
        .pin_bit_mask = (1ULL << dat_gpio),
        .mode = GPIO_MODE_INPUT,
        .pull_up_en = GPIO_PULLUP_DISABLE,
        .pull_down_en = GPIO_PULLDOWN_ENABLE,
        .intr_type = GPIO_INTR_DISABLE,
    };
    gpio_config(&cfg_in);

    /* Wait for DS1302 to settle after power-on */
    ets_delay_us(200);

    /* Only start the oscillator if CH=1 (clock was halted / first power-on).
     * If CH=0 the clock is already running and the stored time is preserved. */
    uint8_t sec = ds_read_reg(0x81);
    bool was_halted = (sec & 0x80) != 0;
    if (was_halted)
    {
        ds_write_reg(0x8E, 0x00); /* remove write-protect */
        ds_write_reg(0x80, 0x00); /* clear CH bit (seconds=0) to start oscillator */
        ds_write_reg(0x82, 0x00); /* minutes = 0 */
        ds_write_reg(0x84, 0x12); /* hours = 12, 24-hour mode */
        ds_write_reg(0x8E, 0x80); /* re-enable write-protect */
    }
    return was_halted;
}

void ds1302_get_time(uint8_t *hour, uint8_t *minute, uint8_t *second)
{
    *hour = bcd2dec(ds_read_reg(0x85) & 0x3F);
    *minute = bcd2dec(ds_read_reg(0x83) & 0x7F);
    *second = bcd2dec(ds_read_reg(0x81) & 0x7F);
}

void ds1302_set_time(uint8_t hour, uint8_t minute, uint8_t second)
{
    ds_write_reg(0x8E, 0x00);
    ds_write_reg(0x80, dec2bcd(second) & 0x7F);
    ds_write_reg(0x82, dec2bcd(minute));
    ds_write_reg(0x84, dec2bcd(hour));
    ds_write_reg(0x8E, 0x80);
}
