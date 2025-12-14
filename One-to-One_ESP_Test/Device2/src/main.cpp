extern "C"
{
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "driver/adc.h"
#include "driver/i2c.h"
#include "esp_wifi.h"
#include "esp_event.h"
#include "esp_log.h"
#include "nvs_flash.h"
#include "esp_timer.h"
}

#include <string>
#include <vector>
#include <algorithm>

#include <LovyanGFX.hpp>

// ======== Display class ========
class LGFX : public lgfx::LGFX_Device
{
    lgfx::Panel_SSD1306 panel;
    lgfx::Bus_I2C bus;

public:
    LGFX()
    {
        auto cfg = bus.config();
        cfg.i2c_port = I2C_NUM_0;
        cfg.freq_write = 400000;
        cfg.pin_sda = 8;
        cfg.pin_scl = 9;
        cfg.i2c_addr = 0x3C;
        bus.config(cfg);
        panel.setBus(&bus);

        auto pcfg = panel.config();
        pcfg.memory_width = 128;
        pcfg.memory_height = 64;
        pcfg.panel_width = 128;
        pcfg.panel_height = 64;
        pcfg.offset_rotation = 2;
        panel.config(pcfg);

        setPanel(&panel);
    }
};

static LGFX display;

// ==== GPIO ====
#define BTN_DOWN GPIO_NUM_5
#define BTN_SELECT GPIO_NUM_6
#define BTN_UP GPIO_NUM_7

#define LED_PIN GPIO_NUM_3

// ==== Light sensor ====
#define LDR_CH ADC1_CHANNEL_0

// ==== Menu ====
int menuIndex = 0;
const int MENU_COUNT = 4;
bool inSub = false;

// ==== WiFi list ====
int wifiCount = 0;
int wifiSel = 0;
int wifiPage = 0;

// ==== Buttons ====
bool lastState[8] = {};
uint32_t pressedAt[8] = {};
const uint32_t longPress = 800;

// ==== Helper function for milliseconds ====
uint32_t get_millis()
{
    return esp_timer_get_time() / 1000;
}

// ========================================================
// ================ Helper functions =======================
// ========================================================

bool readBtn(gpio_num_t pin)
{
    return gpio_get_level(pin) == 0;
}

// returns: 0 none, 1 short, 2 long start, 3 released after long
int btnEvent(gpio_num_t pin)
{
    int idx = pin & 7;
    bool s = readBtn(pin);
    uint32_t now = get_millis();

    if (s && !lastState[idx])
    {
        lastState[idx] = true;
        pressedAt[idx] = now;
        return 0;
    }
    if (!s && lastState[idx])
    {
        lastState[idx] = false;
        uint32_t t = now - pressedAt[idx];
        if (t >= longPress)
            return 3;
        return 1;
    }
    if (s && lastState[idx])
    {
        if (now - pressedAt[idx] >= longPress && pressedAt[idx] != 0xFFFFFFFF)
        {
            pressedAt[idx] = 0xFFFFFFFF;
            return 2;
        }
    }
    return 0;
}

void center(const std::string &t, int y)
{
    int w = display.textWidth(t.c_str());
    display.setCursor((128 - w) / 2, y);
    display.print(t.c_str());
}

// ======== ПРОСТОЙ СКРОЛЛЕР ПО СИМВОЛАМ ========
class TextScroller
{
private:
    int charOffset;
    uint32_t lastUpdate;
    bool isScrolling;

public:
    TextScroller() : charOffset(0), lastUpdate(0), isScrolling(false) {}

    void update(const std::string &text, int width)
    {
        int textWidth = display.textWidth(text.c_str());
        isScrolling = (textWidth > width);

        if (!isScrolling)
        {
            charOffset = 0;
            return;
        }

        uint32_t now = get_millis();
        if (now - lastUpdate > 300)
        { // Медленная прокрутка - 300ms на символ
            charOffset = (charOffset + 1) % text.length();
            lastUpdate = now;
        }
    }

    void draw(const std::string &text, int x, int y, int width)
    {
        if (!isScrolling)
        {
            display.setCursor(x, y);
            display.print(text.c_str());
        }
        else
        {
            // Создаем циклическую строку
            std::string scrollingText = text.substr(charOffset) + "  " + text.substr(0, charOffset);

            // Находим, сколько символов помещается в ширину
            std::string displayText;
            int currentWidth = 0;

            for (char c : scrollingText)
            {
                int charWidth = display.textWidth(std::string(1, c).c_str());
                if (currentWidth + charWidth <= width)
                {
                    displayText += c;
                    currentWidth += charWidth;
                }
                else
                {
                    break;
                }
            }

            display.setCursor(x, y);
            display.print(displayText.c_str());
        }
    }

    void reset()
    {
        charOffset = 0;
        lastUpdate = get_millis();
        isScrolling = false;
    }
};

TextScroller scroller;

// ========================================================
// ======================= MENU ===========================
// ========================================================

void drawMenu()
{
    display.startWrite();
    display.clear();
    display.setFont(&fonts::Font0);
    const char *it[MENU_COUNT] = {"1. MAC", "2. WiFi", "3. LED", "4. Light"};

    for (int i = 0; i < MENU_COUNT; i++)
    {
        display.setCursor(0, i * 10);
        display.printf("%s%s", i == menuIndex ? "> " : "  ", it[i]);
    }
    display.endWrite();
}

// ========================================================
// ======================= MAC ============================
// ========================================================
void showMac()
{
    uint8_t mac[6];
    esp_wifi_get_mac(WIFI_IF_STA, mac);

    char macStr[32];
    sprintf(macStr, "%02X:%02X:%02X:%02X:%02X:%02X",
            mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);

    while (true)
    {
        display.startWrite();
        display.clear();
        display.setCursor(0, 0);
        display.print("MAC:");
        display.setCursor(0, 12);
        display.print(macStr);
        display.setCursor(0, 40);
        display.print("CENTER = back");
        display.endWrite();

        int c = btnEvent(BTN_SELECT);
        if (c == 1 || c == 3)
            return;

        vTaskDelay(100 / portTICK_PERIOD_MS);
    }
}

// ========================================================
// ===================== WiFi Details =====================
// ========================================================
void wifiDetails(int idx, const wifi_ap_record_t &rec)
{
    const char *fields[] = {"SSID", "BSSID", "RSSI", "Chan", "Enc"};
    int field = 0;

    while (true)
    {
        display.startWrite();
        display.clear();
        display.setCursor(0, 0);
        display.printf("Net %d/%d %s", idx + 1, wifiCount, fields[field]);

        char val[64];

        switch (field)
        {
        case 0:
            sprintf(val, "%s", rec.ssid);
            break;
        case 1:
            sprintf(val, "%02X:%02X:%02X:%02X:%02X:%02X",
                    rec.bssid[0], rec.bssid[1], rec.bssid[2],
                    rec.bssid[3], rec.bssid[4], rec.bssid[5]);
            break;
        case 2:
            sprintf(val, "%d", rec.rssi);
            break;
        case 3:
            sprintf(val, "%d", rec.primary);
            break;
        case 4:
            sprintf(val,
                    rec.authmode == WIFI_AUTH_OPEN ? "Open" : rec.authmode == WIFI_AUTH_WPA2_PSK ? "WPA2"
                                                                                                 : "Other");
            break;
        }

        display.setCursor(0, 20);
        display.print(val);

        display.setCursor(0, 50);
        display.print("UP/DN fields   C=back");
        display.endWrite();

        int u = btnEvent(BTN_UP);
        int d = btnEvent(BTN_DOWN);
        int c = btnEvent(BTN_SELECT);

        if (u == 1)
            field = (field + 4) % 5;
        if (d == 1)
            field = (field + 1) % 5;
        if (c == 1 || c == 3)
            return;

        vTaskDelay(100 / portTICK_PERIOD_MS);
    }
}

// ========================================================
// ======================== WiFi List =====================
// ========================================================
void wifiList()
{
    display.startWrite();
    display.clear();
    center("Scanning WiFi...", 20);
    display.endWrite();

    wifi_scan_config_t sc = {};
    esp_wifi_scan_start(&sc, true);

    esp_wifi_scan_get_ap_num((uint16_t *)&wifiCount);

    std::vector<wifi_ap_record_t> list(wifiCount);
    if (wifiCount > 0)
    {
        esp_wifi_scan_get_ap_records((uint16_t *)&wifiCount, list.data());
    }

    wifiSel = 0;
    wifiPage = 0;
    scroller.reset();

    while (true)
    {
        display.startWrite();
        display.clear();

        if (wifiCount == 0)
        {
            center("No networks", 20);
        }
        else
        {
            // Показываем 5 сетей + место для номера страницы
            int lines = std::min(5, wifiCount - wifiPage);
            for (int i = 0; i < lines; i++)
            {
                int idx = wifiPage + i;
                int y = i * 10;

                // Стрелка выбора
                display.setCursor(0, y);
                display.printf("%s", idx == wifiSel ? "> " : "  ");

                // SSID
                std::string ssid = (const char *)list[idx].ssid;

                if (idx == wifiSel)
                {
                    // Прокрутка для выбранного элемента
                    scroller.update(ssid, 85);
                    scroller.draw(ssid, 15, y, 85);
                }
                else
                {
                    // Статический текст для остальных
                    display.setCursor(15, y);
                    std::string displayText = ssid;
                    int textWidth = display.textWidth(ssid.c_str());
                    if (textWidth > 85)
                    {
                        int maxChars = ssid.length();
                        while (maxChars > 3 && display.textWidth(ssid.substr(0, maxChars).c_str()) > 85)
                        {
                            maxChars--;
                        }
                        displayText = ssid.substr(0, maxChars) + "...";
                    }
                    display.print(displayText.c_str());
                }

                // RSSI
                display.setCursor(108, y);
                display.printf("%d", list[idx].rssi);
            }

            // Номер страницы и общее количество
            display.setCursor(0, 56);
            int totalPages = (wifiCount + 4) / 5; // Округление вверх
            int currentPage = (wifiPage / 5) + 1;
            display.printf("Page %d/%d", currentPage, totalPages);

            // Позиция в списке
            display.setCursor(80, 56);
            display.printf("%d/%d", wifiSel + 1, wifiCount);
        }
        display.endWrite();

        int u = btnEvent(BTN_UP);
        int d = btnEvent(BTN_DOWN);
        int c = btnEvent(BTN_SELECT);

        bool selectionChanged = false;

        if (u == 1)
        {
            if (wifiSel > 0)
                wifiSel--;
            if (wifiSel < wifiPage)
                wifiPage = std::max(0, wifiPage - 5);
            selectionChanged = true;
        }
        if (u == 2)
        {
            wifiPage = std::max(0, wifiPage - 5);
            wifiSel = wifiPage;
            selectionChanged = true;
        }

        if (d == 1)
        {
            if (wifiSel < wifiCount - 1)
                wifiSel++;
            if (wifiSel >= wifiPage + 5)
                wifiPage += 5;
            selectionChanged = true;
        }
        if (d == 2)
        {
            if (wifiPage + 5 < wifiCount)
            {
                wifiPage += 5;
                wifiSel = wifiPage;
                selectionChanged = true;
            }
        }

        if (selectionChanged)
        {
            scroller.reset();
        }

        if (c == 1 && wifiCount > 0)
            wifiDetails(wifiSel, list[wifiSel]);
        if (c == 2 || c == 3)
            return;

        vTaskDelay(50 / portTICK_PERIOD_MS);
    }
}

// ========================================================
// ========================= LED ==========================
// ========================================================
void ledMenu()
{
    bool st = gpio_get_level(LED_PIN) == 0;

    while (true)
    {
        display.startWrite();
        display.clear();
        display.setCursor(0, 0);
        display.print("LED:");
        display.setCursor(0, 12);
        display.print(st ? "ON" : "OFF");
        display.setCursor(0, 40);
        display.print("UP/DN toggle");
        display.setCursor(0, 52);
        display.print("CENTER back");
        display.endWrite();

        int u = btnEvent(BTN_UP);
        int d = btnEvent(BTN_DOWN);
        int c = btnEvent(BTN_SELECT);

        if (u == 1 || d == 1)
        {
            st = !st;
            gpio_set_level(LED_PIN, st);
        }

        if (c == 1 || c == 3)
            return;

        vTaskDelay(100 / portTICK_PERIOD_MS);
    }
}

// ========================================================
// ==================== Light sensor ======================
// ========================================================
void showLight()
{
    while (true)
    {
        int raw = adc1_get_raw(LDR_CH);

        char buf[32];
        sprintf(buf, "%d", raw);

        display.startWrite();
        display.clear();
        display.setCursor(0, 0);
        display.print("Light sensor:");
        display.setCursor(0, 12);
        display.print(buf);
        display.setCursor(0, 40);
        display.print("CENTER back");
        display.endWrite();

        int c = btnEvent(BTN_SELECT);
        if (c == 1 || c == 3)
            return;

        vTaskDelay(200 / portTICK_PERIOD_MS);
    }
}

// ========================================================
// ========================= UI TASK ======================
// ========================================================
void uiTask(void *)
{
    drawMenu();

    while (true)
    {
        if (!inSub)
        {
            int u = btnEvent(BTN_UP);
            int d = btnEvent(BTN_DOWN);
            int c = btnEvent(BTN_SELECT);

            if (u == 1)
            {
                menuIndex = (menuIndex - 1 + MENU_COUNT) % MENU_COUNT;
                drawMenu();
            }
            if (d == 1)
            {
                menuIndex = (menuIndex + 1) % MENU_COUNT;
                drawMenu();
            }
            if (c == 1)
            {
                inSub = true;
                switch (menuIndex)
                {
                case 0:
                    showMac();
                    break;
                case 1:
                    wifiList();
                    break;
                case 2:
                    ledMenu();
                    break;
                case 3:
                    showLight();
                    break;
                }
                inSub = false;
                drawMenu();
            }
        }
        vTaskDelay(100 / portTICK_PERIOD_MS);
    }
}

// ========================================================
// ========================= MAIN =========================
// ========================================================
extern "C" void app_main()
{
    nvs_flash_init();

    // GPIO
    gpio_set_direction(BTN_UP, GPIO_MODE_INPUT);
    gpio_set_pull_mode(BTN_UP, GPIO_PULLUP_ONLY);
    gpio_set_direction(BTN_DOWN, GPIO_MODE_INPUT);
    gpio_set_pull_mode(BTN_DOWN, GPIO_PULLUP_ONLY);
    gpio_set_direction(BTN_SELECT, GPIO_MODE_INPUT);
    gpio_set_pull_mode(BTN_SELECT, GPIO_PULLUP_ONLY);

    gpio_set_direction(LED_PIN, GPIO_MODE_OUTPUT);

    // ADC
    adc1_config_width(ADC_WIDTH_BIT_12);
    adc1_config_channel_atten(LDR_CH, ADC_ATTEN_DB_12);

    // WiFi
    esp_netif_init();
    esp_event_loop_create_default();
    esp_netif_create_default_wifi_sta();
    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    esp_wifi_init(&cfg);
    esp_wifi_set_mode(WIFI_MODE_STA);
    esp_wifi_start();

    // Display init
    display.init();
    display.clear();
    display.display();

    // UI task
    xTaskCreate(uiTask, "ui", 8192, nullptr, 1, nullptr);
}