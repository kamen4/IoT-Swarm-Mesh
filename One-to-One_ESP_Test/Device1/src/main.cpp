#include <Adafruit_SSD1306.h>
#include <ESP8266WiFi.h>
#include <espnow.h>

// ==== OLED ====
#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

// ==== Пины ====
#define BUTTON1 D6
#define BUTTON2 D7
#define BUTTON3 D5
#define LDR_PIN A0

void drawButton(int x, int y, int w, int h, const char *label, bool pressed);

void setup()
{
  Serial.begin(9600);
  WiFi.mode(WIFI_STA);
  WiFi.disconnect();

  pinMode(BUTTON1, INPUT_PULLUP);
  pinMode(BUTTON2, INPUT_PULLUP);
  pinMode(BUTTON3, INPUT_PULLUP);

  if (!display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS))
  {
    Serial.println(F("SSD1306 allocation failed"));
    for (;;)
      ;
  }

  display.clearDisplay();
  display.display();
}

void loop()
{
  // --- кнопки ---
  bool b1 = digitalRead(BUTTON1) == LOW;
  bool b2 = digitalRead(BUTTON2) == LOW;
  bool b3 = digitalRead(BUTTON3) == LOW;

  // --- освещенность ---
  int light = analogRead(LDR_PIN);

  // --- экран ---
  display.clearDisplay();
  display.setTextColor(SSD1306_WHITE);
  display.setTextSize(1, 2);

  // MAC в две строки
  String mac = WiFi.macAddress();
  display.setCursor(0, 0);
  display.print("MAC:");
  display.setCursor(0, 16);
  display.print(mac.substring(0, 8));
  display.setCursor(0, 32);
  display.print(mac.substring(9));

  // Освещенность
  display.setCursor(0, 48);
  display.print("L:");
  display.print(light);

  // --- кнопки ---
  drawButton(80, 48, 14, 14, "1", b1);
  drawButton(96, 48, 14, 14, "2", b2);
  drawButton(112, 48, 14, 14, "3", b3);

  display.display();
  delay(20); // минимальная стабилизация экрана
}

void drawButton(int x, int y, int w, int h, const char *label, bool pressed)
{
  if (pressed)
  {
    display.fillRect(x, y, w, h, SSD1306_WHITE);
    display.setTextColor(SSD1306_BLACK);
  }
  else
  {
    display.drawRect(x, y, w, h, SSD1306_WHITE);
    display.setTextColor(SSD1306_WHITE);
  }

  display.setTextSize(1, 2);
  display.setCursor(x + 3, y + 1);
  display.print(label);
}
