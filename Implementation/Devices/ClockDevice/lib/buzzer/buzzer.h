/**
 * @file buzzer.h
 * @brief Public API for the passive-buzzer driver.
 *
 * Uses the ESP32 LEDC peripheral (timer 0, channel 0) to generate
 * PWM tones.  A passive buzzer requires a frequency signal to produce
 * sound; a simple HIGH/LOW level has no effect on passive buzzers.
 */
#pragma once
#include <stdint.h>

/** Configure LEDC timer 0 and channel 0 for the given GPIO.
 *  Must be called once at startup before any beep function. */
void buzzer_init(int gpio);

/** Play a single blocking tone at freq_hz for dur_ms milliseconds. */
void buzzer_tone(uint32_t freq_hz, uint32_t dur_ms);

/** Short single beep — generic key-press feedback. */
void buzzer_beep_short(void);
/** Rising two-tone — played when settings are saved. */
void buzzer_beep_confirm(void);
/** Low long tone — played when exiting without saving. */
void buzzer_beep_cancel(void);
/** Double beep — played at startup and every 15 minutes. */
void buzzer_beep_quarter(void);
/** 7-note ascending/descending melody — played at every full hour. */
void buzzer_play_hour_melody(void);
