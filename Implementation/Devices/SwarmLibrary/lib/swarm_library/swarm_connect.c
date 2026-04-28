/* swarm_connect.c -- Onboarding captive-portal implementation.
 *
 * Wi-Fi Soft-AP  +  minimal DNS server (UDP/53)  +  HTTP captive portal.
 *
 * Protocol references:
 *   Protocol/_docs_v1.0/algorithms/01-onboarding.md
 *   Protocol/_docs_v1.0/00-glossary.md  (CONNECTION_STRING, CONNECTION_KEY)
 */

#include "swarm_connect.h"

#include "esp_err.h"
#include "esp_event.h"
#include "esp_http_server.h"
#include "esp_log.h"
#include "esp_mac.h"
#include "esp_netif.h"
#include "esp_random.h"
#include "esp_wifi.h"
#include "nvs_flash.h"

#include "mbedtls/base64.h"
#include "mbedtls/sha256.h"

#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

#include "lwip/sockets.h"

#include <stdio.h>
#include <string.h>

/* ---------------------------------------------------------------------------
 * Module constants
 * --------------------------------------------------------------------------- */

#define TAG "swarm_connect"

/** Default ESP Soft-AP gateway IP (assigned by the built-in DHCP server). */
#define AP_IP_STR "192.168.4.1"

#define DNS_PORT 53
#define DNS_BUF_SIZE 512

/** Length of the random CONNECTION_KEY in bytes. */
#define CONNECTION_KEY_LEN 32u

/** "AA:BB:CC:DD:EE:FF\0" */
#define MAC_STR_LEN 18u

/** base64(32 bytes) = 44 printable chars + '\0'. */
#define BASE64_SHA_LEN 45u

/** MAC (17) + ':' (1) + base64 (44) + '\0' = 63 bytes; 80 gives headroom. */
#define CS_BUF_LEN 80u

/* ---------------------------------------------------------------------------
 * HTML captive-portal page template
 *
 * Two positional %s format specifiers:
 *   1st %s → device MAC address string
 *   2nd %s → CONNECTION_STRING
 *
 * Design goals:
 *   - Mobile-first layout (viewport meta, max-width).
 *   - The CONNECTION_STRING box has user-select:all for easy long-press copy.
 *   - A JavaScript copy button with clipboard API + textarea fallback.
 * --------------------------------------------------------------------------- */

static const char HTML_TEMPLATE[] =
    "<!DOCTYPE html><html><head>"
    "<meta charset='UTF-8'>"
    "<meta name='viewport' content='width=device-width,initial-scale=1'>"
    "<title>IoT Swarm Device</title>"
    "<style>"
    "body{font-family:sans-serif;max-width:480px;margin:32px auto;"
    "padding:0 20px;color:#222}"
    "h1{font-size:1.4em;margin-bottom:4px}"
    "p{color:#555;font-size:.9em;margin:0 0 20px}"
    ".lbl{font-size:.75em;color:#777;margin-top:14px}"
    ".box{background:#f4f4f4;border-radius:6px;padding:10px 14px;"
    "margin-top:4px;font-family:monospace;font-size:.85em;"
    "word-break:break-all;user-select:all}"
    ".btn{margin-top:20px;padding:10px 24px;background:#1976D2;"
    "color:#fff;border:none;border-radius:6px;cursor:pointer;"
    "font-size:.9em;display:block}"
    "</style></head><body>"
    "<h1>IoT Swarm Device</h1>"
    "<p>Copy the CONNECTION_STRING and send it to the Telegram bot "
    "to register this device.</p>"
    "<div class='lbl'>MAC Address</div>"
    "<div class='box'>%s</div>"
    "<div class='lbl'>CONNECTION_STRING</div>"
    "<div class='box' id='cs'>%s</div>"
    "<button id='cb' class='btn' onclick='doCopy()'>"
    "Copy CONNECTION_STRING</button>"
    "<script>"
    "function doCopy(){"
    "var t=document.getElementById('cs').innerText;"
    "if(navigator.clipboard){navigator.clipboard.writeText(t);}"
    "else{"
    "var a=document.createElement('textarea');"
    "a.value=t;document.body.appendChild(a);a.select();"
    "document.execCommand('copy');document.body.removeChild(a);}"
    "var b=document.getElementById('cb');"
    "b.textContent='Copied!';"
    "setTimeout(function(){b.textContent='Copy CONNECTION_STRING'},2000);}"
    "</script></body></html>";

/* ---------------------------------------------------------------------------
 * Module state
 * --------------------------------------------------------------------------- */

static bool s_initialized = false;
static volatile bool s_dns_running = false;
static httpd_handle_t s_http_server = NULL;
static TaskHandle_t s_dns_task_handle = NULL;

/** Device SoftAP MAC, e.g. "AA:BB:CC:DD:EE:FF". */
static char s_mac_str[MAC_STR_LEN];

/** Full CONNECTION_STRING ready for display and copy. */
static char s_connection_string[CS_BUF_LEN];

/* ---------------------------------------------------------------------------
 * Internal: derive CONNECTION_STRING
 * --------------------------------------------------------------------------- */

/**
 * Generate a random CONNECTION_KEY, compute SHA-256, base64-encode it, and
 * combine with the device SoftAP MAC to produce CONNECTION_STRING.
 *
 * CONNECTION_STRING format (protocol glossary):
 *   <AA:BB:CC:DD:EE:FF>:<base64(SHA256(CONNECTION_KEY))>
 */
static esp_err_t derive_connection_string(void)
{
    /* 1. Generate 32 random bytes as the CONNECTION_KEY.
     *    esp_random() is the hardware RNG; called in a loop to fill the buffer
     *    without depending on esp_fill_random() availability. */
    uint8_t connection_key[CONNECTION_KEY_LEN];
    for (size_t i = 0; i < sizeof(connection_key); i += sizeof(uint32_t))
    {
        uint32_t r = esp_random();
        size_t n = sizeof(connection_key) - i;
        memcpy(connection_key + i, &r, n < sizeof(uint32_t) ? n : sizeof(uint32_t));
    }

    /* 2. SHA-256(CONNECTION_KEY) → 32-byte digest. */
    uint8_t sha_out[32];
    mbedtls_sha256(connection_key, sizeof(connection_key), sha_out, 0 /* SHA-256 */);

    /* 3. Base64-encode the 32-byte digest → up to 44 printable chars. */
    unsigned char base64_buf[BASE64_SHA_LEN];
    size_t base64_written = 0;
    if (mbedtls_base64_encode(base64_buf, sizeof(base64_buf),
                              &base64_written, sha_out, sizeof(sha_out)) != 0)
    {
        ESP_LOGE(TAG, "base64 encode failed");
        return ESP_FAIL;
    }
    base64_buf[base64_written] = '\0';

    /* 4. Read device SoftAP MAC address. */
    uint8_t mac[6];
    esp_err_t err = esp_read_mac(mac, ESP_MAC_WIFI_SOFTAP);
    if (err != ESP_OK)
    {
        ESP_LOGE(TAG, "esp_read_mac failed: %s", esp_err_to_name(err));
        return err;
    }
    snprintf(s_mac_str, sizeof(s_mac_str),
             "%02X:%02X:%02X:%02X:%02X:%02X",
             mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);

    /* 5. Build CONNECTION_STRING = <MAC>:<base64(SHA256(CONNECTION_KEY))>. */
    snprintf(s_connection_string, sizeof(s_connection_string),
             "%s:%s", s_mac_str, (char *)base64_buf);

    ESP_LOGI(TAG, "MAC:               %s", s_mac_str);
    ESP_LOGI(TAG, "CONNECTION_STRING: %s", s_connection_string);
    return ESP_OK;
}

/* ---------------------------------------------------------------------------
 * Internal: minimal DNS server task
 * --------------------------------------------------------------------------- */

/**
 * FreeRTOS task: UDP DNS server on port 53.
 *
 * Resolves every incoming DNS A query to the AP gateway IP (AP_IP_STR) so
 * that OS captive-portal detectors are triggered regardless of which domain
 * they query.
 *
 * DNS response construction:
 *   - Header: QR=1, AA=1, RD=1, RA=1, RCODE=0; ANCOUNT=1.
 *   - Question section: copied verbatim from the query.
 *   - Answer: name pointer 0xC00C (byte 12), type A, class IN, TTL 60,
 *             RDATA = 4 bytes of AP_IP_STR.
 */
static void dns_server_task(void *arg)
{
    int sock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (sock < 0)
    {
        ESP_LOGE(TAG, "DNS socket() failed: errno %d", errno);
        vTaskDelete(NULL);
        return;
    }

    /* Receive timeout so the loop can re-check s_dns_running each second. */
    struct timeval tv = {.tv_sec = 1, .tv_usec = 0};
    setsockopt(sock, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof(tv));

    struct sockaddr_in sa = {
        .sin_family = AF_INET,
        .sin_port = htons(DNS_PORT),
        .sin_addr.s_addr = htonl(INADDR_ANY),
    };
    if (bind(sock, (struct sockaddr *)&sa, sizeof(sa)) < 0)
    {
        ESP_LOGE(TAG, "DNS bind() failed: errno %d", errno);
        close(sock);
        vTaskDelete(NULL);
        return;
    }

    /* Resolve AP IP once into network-byte-order for the answer RDATA. */
    struct in_addr ap_addr;
    inet_aton(AP_IP_STR, &ap_addr);

    uint8_t buf[DNS_BUF_SIZE];
    uint8_t rsp[DNS_BUF_SIZE];
    struct sockaddr_in client;
    socklen_t client_len;

    while (s_dns_running)
    {
        client_len = sizeof(client);
        int len = recvfrom(sock, buf, sizeof(buf) - 1, 0,
                           (struct sockaddr *)&client, &client_len);

        /* Timeout or error: just loop back to check s_dns_running. */
        if (len < 12)
            continue;

        /* ---- Build response header (first 12 bytes) ---- */
        memcpy(rsp, buf, 12);
        rsp[2] = 0x81; /* QR=1, OPCODE=0, AA=1, TC=0, RD=1               */
        rsp[3] = 0x80; /* RA=1, Z=0, RCODE=0 (no error)                  */
        /* rsp[4..5]: QDCOUNT copied from query (keep the question count).  */
        rsp[6] = 0x00; /* ANCOUNT high byte                               */
        rsp[7] = 0x01; /* ANCOUNT = 1                                     */
        rsp[8] = 0x00; /* NSCOUNT = 0                                     */
        rsp[9] = 0x00;
        rsp[10] = 0x00; /* ARCOUNT = 0                                     */
        rsp[11] = 0x00;

        /* ---- Walk QNAME to locate the end of the question section ---- */
        int pos = 12;
        while (pos < len)
        {
            uint8_t label = buf[pos];
            if (label == 0)
            {
                pos++;
                break;
            } /* root label */
            if ((label & 0xC0) == 0xC0)
            {
                pos += 2;
                break;
            } /* DNS ptr   */
            pos += label + 1;
        }
        /* Skip QTYPE (2 bytes) + QCLASS (2 bytes). */
        int q_end = pos + 4;
        if (q_end > len)
            continue; /* truncated query — skip */

        /* ---- Copy the question section into the response ---- */
        int q_len = q_end - 12;
        /* Overflow guard: answer record is 16 bytes; leave room. */
        if (12 + q_len + 16 > (int)sizeof(rsp))
            continue;
        memcpy(rsp + 12, buf + 12, q_len);

        /* ---- Append answer record ---- */
        int ans = 12 + q_len;
        rsp[ans++] = 0xC0; /* Name: DNS pointer to byte 12 (QNAME in question) */
        rsp[ans++] = 0x0C;
        rsp[ans++] = 0x00;
        rsp[ans++] = 0x01; /* Type A                        */
        rsp[ans++] = 0x00;
        rsp[ans++] = 0x01; /* Class IN                      */
        rsp[ans++] = 0x00;
        rsp[ans++] = 0x00; /* TTL high word                 */
        rsp[ans++] = 0x00;
        rsp[ans++] = 0x3C; /* TTL = 60 seconds              */
        rsp[ans++] = 0x00;
        rsp[ans++] = 0x04; /* RDLENGTH = 4                  */
        /* RDATA: AP IP address (already in network byte order). */
        memcpy(rsp + ans, &ap_addr.s_addr, 4);
        ans += 4;

        sendto(sock, rsp, ans, 0, (struct sockaddr *)&client, client_len);
    }

    close(sock);
    vTaskDelete(NULL);
}

/* ---------------------------------------------------------------------------
 * Internal: HTTP handlers
 * --------------------------------------------------------------------------- */

/**
 * GET / — serve the device info page.
 *
 * Renders HTML_TEMPLATE substituting the MAC address and CONNECTION_STRING.
 */
static esp_err_t http_index_handler(httpd_req_t *req)
{
    /* Allocate buffer: template size + space for the two substitutions. */
    size_t page_size = sizeof(HTML_TEMPLATE) + MAC_STR_LEN + CS_BUF_LEN;
    char *page = malloc(page_size);
    if (!page)
    {
        httpd_resp_send_err(req, HTTPD_500_INTERNAL_SERVER_ERROR,
                            "Out of memory");
        return ESP_FAIL;
    }
    snprintf(page, page_size, HTML_TEMPLATE, s_mac_str, s_connection_string);

    httpd_resp_set_type(req, "text/html");
    httpd_resp_send(req, page, (ssize_t)strlen(page));
    free(page);
    return ESP_OK;
}

/**
 * Catch-all 404 error handler: redirect to the index page.
 *
 * This handles captive-portal probes from all major OS families:
 *   Android  — /generate_204, /connectivity-check.html
 *   iOS/macOS — /hotspot-detect.html, /success.html
 *   Windows  — /ncsi.txt, /connecttest.txt, /redirect
 * All of them receive a 302 pointing to the device info page.
 */
static esp_err_t http_redirect_handler(httpd_req_t *req, httpd_err_code_t err)
{
    httpd_resp_set_status(req, "302 Found");
    httpd_resp_set_hdr(req, "Location", "http://" AP_IP_STR "/");
    httpd_resp_send(req, NULL, 0);
    return ESP_OK;
}

/* ---------------------------------------------------------------------------
 * Internal: start HTTP server
 * --------------------------------------------------------------------------- */

/** Configure and start the esp_http_server with the captive portal handlers. */
static esp_err_t start_http_server(void)
{
    httpd_config_t cfg = HTTPD_DEFAULT_CONFIG();
    cfg.max_uri_handlers = 8;

    if (httpd_start(&s_http_server, &cfg) != ESP_OK)
    {
        ESP_LOGE(TAG, "httpd_start() failed");
        return ESP_FAIL;
    }

    /* Main device info page. */
    static const httpd_uri_t index_uri = {
        .uri = "/",
        .method = HTTP_GET,
        .handler = http_index_handler,
    };
    httpd_register_uri_handler(s_http_server, &index_uri);

    /* Redirect all unrecognised paths to the index page. */
    httpd_register_err_handler(s_http_server, HTTPD_404_NOT_FOUND,
                               http_redirect_handler);
    return ESP_OK;
}

/* ---------------------------------------------------------------------------
 * Public API
 * --------------------------------------------------------------------------- */

esp_err_t swarm_connect_init(void)
{
    if (s_initialized)
        return ESP_OK;

    esp_err_t ret;

    /* 1. NVS flash — required by the Wi-Fi driver. */
    ret = nvs_flash_init();
    if (ret == ESP_ERR_NVS_NO_FREE_PAGES ||
        ret == ESP_ERR_NVS_NEW_VERSION_FOUND)
    {
        ESP_LOGW(TAG, "NVS partition needs erase, erasing...");
        ESP_ERROR_CHECK(nvs_flash_erase());
        ret = nvs_flash_init();
    }
    if (ret != ESP_OK)
    {
        ESP_LOGE(TAG, "nvs_flash_init failed: %s", esp_err_to_name(ret));
        return ret;
    }

    /* 2. TCP/IP stack. */
    ret = esp_netif_init();
    if (ret != ESP_OK && ret != ESP_ERR_INVALID_STATE)
    {
        ESP_LOGE(TAG, "esp_netif_init failed: %s", esp_err_to_name(ret));
        return ret;
    }

    /* 3. Default event loop (needed by esp_wifi). */
    ret = esp_event_loop_create_default();
    if (ret != ESP_OK && ret != ESP_ERR_INVALID_STATE)
    {
        ESP_LOGE(TAG, "esp_event_loop_create_default failed: %s",
                 esp_err_to_name(ret));
        return ret;
    }

    /* 4. Create the default Soft-AP network interface. */
    esp_netif_create_default_wifi_ap();

    /* 5. Initialise the Wi-Fi driver. */
    wifi_init_config_t wifi_cfg = WIFI_INIT_CONFIG_DEFAULT();
    ret = esp_wifi_init(&wifi_cfg);
    if (ret != ESP_OK)
    {
        ESP_LOGE(TAG, "esp_wifi_init failed: %s", esp_err_to_name(ret));
        return ret;
    }

    /* 6. Configure and start the Soft-AP (open network for easy user access). */
    wifi_config_t ap_config = {
        .ap = {
            .ssid = SWARM_CONNECT_AP_SSID,
            .ssid_len = (uint8_t)strlen(SWARM_CONNECT_AP_SSID),
            .channel = SWARM_CONNECT_AP_CHANNEL,
            .password = "",
            .max_connection = SWARM_CONNECT_AP_MAX_STA,
            .authmode = WIFI_AUTH_OPEN,
            .beacon_interval = 100,
        },
    };
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_AP));
    ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_AP, &ap_config));
    ESP_ERROR_CHECK(esp_wifi_start());
    ESP_LOGI(TAG, "Soft-AP started, SSID='%s'", SWARM_CONNECT_AP_SSID);

    /* 7. Derive CONNECTION_STRING from a fresh random CONNECTION_KEY. */
    ret = derive_connection_string();
    if (ret != ESP_OK)
        return ret;

    /* 8. Start the DNS redirect server. */
    s_dns_running = true;
    if (xTaskCreate(dns_server_task, "swarm_dns", 4096, NULL, 5,
                    &s_dns_task_handle) != pdPASS)
    {
        ESP_LOGE(TAG, "Failed to create DNS task");
        s_dns_running = false;
        return ESP_FAIL;
    }

    /* 9. Start the HTTP captive-portal server. */
    ret = start_http_server();
    if (ret != ESP_OK)
        return ret;

    s_initialized = true;
    ESP_LOGI(TAG, "Captive portal ready at http://" AP_IP_STR "/");
    return ESP_OK;
}

esp_err_t swarm_connect_get_connection_string(char *buf, size_t buf_len)
{
    if (!buf)
        return ESP_ERR_INVALID_ARG;
    if (!s_initialized)
        return ESP_ERR_INVALID_STATE;

    snprintf(buf, buf_len, "%s", s_connection_string);
    return ESP_OK;
}

void swarm_connect_stop(void)
{
    if (!s_initialized)
        return;

    /* Stop HTTP server. */
    if (s_http_server)
    {
        httpd_stop(s_http_server);
        s_http_server = NULL;
    }

    /* Signal DNS task to exit and allow up to 1.5 s for the socket timeout. */
    s_dns_running = false;
    if (s_dns_task_handle)
    {
        vTaskDelay(pdMS_TO_TICKS(1500));
        s_dns_task_handle = NULL;
    }

    /* Stop and deinit Wi-Fi. */
    esp_wifi_stop();
    esp_wifi_deinit();

    s_initialized = false;
    ESP_LOGI(TAG, "Captive portal stopped");
}
