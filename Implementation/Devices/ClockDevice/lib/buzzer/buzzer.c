/**
 * @file buzzer.c
 * @brief Passive-buzzer driver using ESP32 LEDC.
 *
 * LEDC resource allocation:
 *   Timer   : LEDC_TIMER_0   (8-bit resolution)
 *   Channel : LEDC_CHANNEL_0
 *   Duty    : 128 / 255 (50 % square wave) while tone is playing, 0 when idle.
 */
#include "buzzer.h"
#include "driver/ledc.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

/* Musical note frequencies in Hz */
#define N_E4 330
#define N_G4 392
#define N_A4 440
#define N_C5 523

void buzzer_init(int gpio)
{
    ledc_timer_config_t tmr = {
        .speed_mode = LEDC_LOW_SPEED_MODE,
        .timer_num = LEDC_TIMER_0,
        .duty_resolution = LEDC_TIMER_8_BIT,
        .freq_hz = 2000,
        .clk_cfg = LEDC_AUTO_CLK,
    };
    ledc_timer_config(&tmr);

    ledc_channel_config_t ch = {
        .gpio_num = gpio,
        .speed_mode = LEDC_LOW_SPEED_MODE,
        .channel = LEDC_CHANNEL_0,
        .timer_sel = LEDC_TIMER_0,
        .duty = 0,
        .hpoint = 0,
    };
    ledc_channel_config(&ch);
}

void buzzer_tone(uint32_t freq_hz, uint32_t dur_ms)
{
    ledc_set_freq(LEDC_LOW_SPEED_MODE, LEDC_TIMER_0, freq_hz);
    ledc_set_duty(LEDC_LOW_SPEED_MODE, LEDC_CHANNEL_0, 128);
    ledc_update_duty(LEDC_LOW_SPEED_MODE, LEDC_CHANNEL_0);
    vTaskDelay(pdMS_TO_TICKS(dur_ms));
    ledc_set_duty(LEDC_LOW_SPEED_MODE, LEDC_CHANNEL_0, 0);
    ledc_update_duty(LEDC_LOW_SPEED_MODE, LEDC_CHANNEL_0);
}

void buzzer_beep_short(void)
{
    buzzer_tone(2000, 60);
}

void buzzer_beep_confirm(void)
{
    buzzer_tone(1800, 100);
    vTaskDelay(pdMS_TO_TICKS(60));
    buzzer_tone(2200, 120);
}

void buzzer_beep_cancel(void)
{
    buzzer_tone(800, 200);
}

void buzzer_beep_quarter(void)
{
    buzzer_tone(1800, 120);
    vTaskDelay(pdMS_TO_TICKS(80));
    buzzer_tone(1800, 120);
}

void buzzer_play_hour_melody(void)
{
    buzzer_tone(N_E4, 180);
    vTaskDelay(pdMS_TO_TICKS(60));
    buzzer_tone(N_G4, 180);
    vTaskDelay(pdMS_TO_TICKS(60));
    buzzer_tone(N_A4, 180);
    vTaskDelay(pdMS_TO_TICKS(60));
    buzzer_tone(N_C5, 360);
    vTaskDelay(pdMS_TO_TICKS(80));
    buzzer_tone(N_A4, 180);
    vTaskDelay(pdMS_TO_TICKS(60));
    buzzer_tone(N_G4, 180);
    vTaskDelay(pdMS_TO_TICKS(60));
    buzzer_tone(N_E4, 360);
}
