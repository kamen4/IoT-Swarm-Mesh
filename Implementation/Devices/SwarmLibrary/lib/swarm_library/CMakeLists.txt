/* ESP-IDF component registration for swarm_library.
 * Compiled automatically by PlatformIO as part of ESP-IDF build. */
idf_component_register(
    SRCS
        "swarm_blink.c"
        "swarm_connect.c"
    INCLUDE_DIRS "include"
    REQUIRES
        "driver"
        "esp_event"
        "esp_http_server"
        "esp_netif"
        "esp_wifi"
        "lwip"
        "mbedtls"
        "nvs_flash"
)
