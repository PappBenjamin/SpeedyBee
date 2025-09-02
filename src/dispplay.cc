#include "display.h"


void displayInit()
{
  if (!display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS))
  {
    Serial1.println("SSD1306 initialization failed!");
    while (true)
      ;
  }
}

void displayClear()
{
    display.clearDisplay();
    display.display();
}

void displayPrint(const char *text)
{
  display.clearDisplay();
  display.setTextSize(2.5);
  display.setTextColor(SSD1306_WHITE);
  display.setCursor(0, 29.5);
  display.println(text);
  display.display();

}
