#include "includes.h"
#include "defines.h"

// IO Expander
Adafruit_MCP23X17 mcp;
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);


// QTR Sensors
const uint8_t QTRPins[] = {14, 13, 12, 11, 10};
QTRSensors qtr;
int QTRSensorCount = 5;


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






  qtr.setTypeRC();
  qtr.setSensorPins(QTRPins, QTRSensorCount);

  for (int i = 0; i < 300; i++)
  {
    qtr.calibrate();
  }


}

void loop()
{
  int KeypadNum = checkExpanderInterrupt(mcp);
  if(KeypadNum != -1){
    // TODO: handle interrupt
  }

  u16_t QTRSensorValues[5];
  qtr.read(QTRSensorValues);

  for (int i = 0; i < QTRSensorCount; i++)
  {
    Serial.print("QTR Sensor ");
    Serial.print(i);
    Serial.print(": ");
    Serial.println(QTRSensorValues[i]);
  }

  delay(100); // Delay for readability
}
