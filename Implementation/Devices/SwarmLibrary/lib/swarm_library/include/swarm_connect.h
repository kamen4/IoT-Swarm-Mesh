/* swarm_connect.h -- Onboarding captive-portal module for SwarmLibrary.
 *
 * Creates a Wi-Fi Soft-AP and a captive portal (DNS + HTTP) so that any
 * device connecting to the AP is automatically redirected to a page that
 * displays the device MAC address and the CONNECTION_STRING required by
 * the onboarding flow.
 *
 * Protocol reference:
 *   Protocol/_docs_v1.0/algorithms/01-onboarding.md
 *   Protocol/_docs_v1.0/00-glossary.md  (CONNECTION_STRING, CONNECTION_KEY)
 *
 * Onboarding flow summary:
 *   1. Device boots and calls swarm_connect_init().
 *   2. User connects to the SSID broadcast by the device (SWARM_CONNECT_AP_SSID).
 *   3. The OS captive-portal detector fires; a mini-browser opens and is
 *      automatically redirected to the device info page showing:
 *        - Device MAC address (visual identification).
 *        - CONNECTION_STRING = <MAC>:<base64(SHA256(CONNECTION_KEY))>
 *   4. User copies the CONNECTION_STRING and sends it to the Telegram bot.
 *   5. After the server completes onboarding, the application calls
 *      swarm_connect_stop() and switches to normal mesh operation.
 *
 * The CONNECTION_KEY is a 32-byte random secret generated at each boot.
 * It never leaves the device; only its SHA-256 hash (base64-encoded) is
 * embedded in the CONNECTION_STRING shown to the user, as required by the
 * protocol SPAKE2 onboarding spec.
 */
#pragma once

#include "esp_err.h"
#include <stddef.h>

/* ---------------------------------------------------------------------------
 * Configuration constants (may be overridden before including this header)
 * --------------------------------------------------------------------------- */

/** Wi-Fi Soft-AP SSID broadcast during onboarding. */
#ifndef SWARM_CONNECT_AP_SSID
#define SWARM_CONNECT_AP_SSID "SwarmDevice"
#endif

/** 2.4 GHz channel used for the onboarding AP (1–13). */
#ifndef SWARM_CONNECT_AP_CHANNEL
#define SWARM_CONNECT_AP_CHANNEL 1
#endif

/** Maximum simultaneous STA connections to the onboarding AP. */
#ifndef SWARM_CONNECT_AP_MAX_STA
#define SWARM_CONNECT_AP_MAX_STA 4
#endif

/* ---------------------------------------------------------------------------
 * Public API
 * --------------------------------------------------------------------------- */

/**
 * Initialise and start the onboarding captive portal.
 *
 * Performs, in order:
 *   1. Generates a cryptographically random 32-byte CONNECTION_KEY.
 *   2. Derives CONNECTION_STRING = <MAC>:<base64(SHA256(CONNECTION_KEY))>.
 *   3. Initialises NVS flash, esp_netif, and Wi-Fi subsystems if not done.
 *   4. Starts Wi-Fi Soft-AP with SSID SWARM_CONNECT_AP_SSID (open, no password).
 *   5. Starts a minimal DNS server (UDP/53) that resolves every domain to the
 *      AP gateway IP (192.168.4.1), triggering the OS captive-portal detector.
 *   6. Starts an HTTP server (TCP/80) that serves the device info page at /
 *      and redirects all other requests there (handles Android /generate_204,
 *      iOS /hotspot-detect.html, Windows /ncsi.txt, etc.).
 *
 * Calling this function more than once has no effect after the first call.
 *
 * @return ESP_OK on success, or an esp_err_t error code on failure.
 */
esp_err_t swarm_connect_init(void);

/**
 * Copy the current CONNECTION_STRING into the provided buffer.
 *
 * CONNECTION_STRING format (protocol spec):
 *   <AA:BB:CC:DD:EE:FF>:<base64(SHA256(CONNECTION_KEY))>
 *
 * @param buf     Destination buffer; recommend at least 80 bytes.
 * @param buf_len Size of the destination buffer in bytes.
 * @return ESP_OK on success.
 *         ESP_ERR_INVALID_ARG  if buf is NULL.
 *         ESP_ERR_INVALID_STATE if swarm_connect_init() has not been called.
 */
esp_err_t swarm_connect_get_connection_string(char *buf, size_t buf_len);

/**
 * Stop the captive portal and tear down the Wi-Fi Soft-AP.
 *
 * Stops the HTTP server, signals the DNS task to exit, stops and deinits
 * the Wi-Fi driver. Safe to call even if swarm_connect_init() was never called.
 */
void swarm_connect_stop(void);
