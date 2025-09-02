#include "includes.h"
#include "defines.h"

// IO Expander
Adafruit_MCP23X17 mcp;

// Display
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);


// QTR Sensors
QTRSensors qtr;


void setup()
{
  Serial.begin(115200);
  Wire.begin();

  delay(200);

  pinMode(AIN1, OUTPUT);
  pinMode(AIN2, OUTPUT);
  pinMode(PWMA, OUTPUT);

  pinMode(BIN1, OUTPUT);
  pinMode(BIN2, OUTPUT);
  pinMode(PWMB, OUTPUT);

  pinMode(BUZZER, OUTPUT);

  //Buzzer
  playStartSong();


  // IO Expander
  setupExpander(mcp);

  // // display

  if (!display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS))
  {
    Serial1.println("SSD1306 initialization failed!");
    while (true)
      ;
  }

  // draw something
  display.clearDisplay();
  display.setTextSize(2.5);
  display.setTextColor(SSD1306_WHITE);
  display.setCursor(0, 29.5);
  display.println("SpeedyBee!");

  display.display();



  // QTR Sensors
  qtrCalibrate(qtr);

}


void loop()
{
  int KeypadNum = checkExpanderInterrupt(mcp);
  if(KeypadNum != -1){
    // TODO: handle interrupt
  }


  u16_t QTRSensorValues[5];
  readQTRSensors(qtr, QTRSensorValues);
  printQTRSensorValues(QTRSensorValues);

  delay(100); // Delay for readability
}
