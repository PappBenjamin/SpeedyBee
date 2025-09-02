#include "main_defs_includes.h"


// IO Expander
Adafruit_MCP23X17 mcp;


// QTR Sensors
const uint8_t QTRPins[] = {14, 13, 12, 11, 10};
QTRSensors qtr;
int QTRSensorCount = 5;


void setup()
{
  Serial.begin(115200);

  delay(200);

  Serial.println("Serial library initialized.");

  if (!mcp.begin_I2C(IO_ADDRESS))
  {
    Serial.println("Error.");
    while (1)
      ;
  }

  Serial.println("IO Expander found!");

  pinMode(AIN1, OUTPUT);
  pinMode(AIN2, OUTPUT);
  pinMode(PWMA, OUTPUT);

  pinMode(BIN1, OUTPUT);
  pinMode(BIN2, OUTPUT);
  pinMode(PWMB, OUTPUT);

  pinMode(BUZZER, OUTPUT);

  pinMode(INT_A, INPUT);

  // IO Expander
  mcp.setupInterrupts(true, false, LOW);

  // configure button pin for input with pull up
  mcp.pinMode(0, INPUT_PULLUP);
  mcp.pinMode(1, INPUT_PULLUP);
  mcp.pinMode(2, INPUT_PULLUP);
  mcp.pinMode(3, INPUT_PULLUP);
  mcp.pinMode(4, INPUT_PULLUP);
  mcp.pinMode(5, INPUT_PULLUP);

  // enable interrupt on button_pin
  mcp.setupInterruptPin(0, LOW);
  mcp.setupInterruptPin(1, LOW);
  mcp.setupInterruptPin(2, LOW);
  mcp.setupInterruptPin(3, LOW);
  mcp.setupInterruptPin(4, LOW);
  mcp.setupInterruptPin(5, LOW);

  // Buzzer for sound alerts
  int size = sizeof(durations) / sizeof(int);

  for (int note = 0; note < size; note++)
  {
    // to calculate the note duration, take one second divided by the note type.
    // e.g. quarter note = 1000 / 4, eighth note = 1000/8, etc.
    int duration = 1000 / durations[note];
    tone(BUZZER, melody[note], duration);

    // to distinguish the notes, set a minimum time between them.
    // the note's duration + 30% seems to work well:
    int pauseBetweenNotes = duration * 1.30;
    delay(pauseBetweenNotes);

    // stop the tone playing:
    noTone(BUZZER);
  }

  // // display
  Wire.begin();

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

  Serial.println("SSD1306 initialized successfully.");





  qtr.setTypeRC();
  qtr.setSensorPins(QTRPins, QTRSensorCount);

  for (int i = 0; i < 300; i++)
  {
    qtr.calibrate();
  }


}

void loop()
{

  // IO Expander
  if (!digitalRead(INT_A))
  {
    Serial.print("Interrupt detected on pin: ");
    Serial.println(mcp.getLastInterruptPin());
    mcp.clearInterrupts(); // clear
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

  delay(10); // Delay for readability
}
