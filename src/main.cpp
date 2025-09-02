#include "main_defs_includes.h"
#include "expander.h"

// IO Expander
Adafruit_MCP23X17 mcp;
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);


// QTR Sensors
const uint8_t QTRPins[] = {14, 13, 12, 11, 10};
QTRSensors qtr;
int QTRSensorCount = 5;


// melody here

int melody[] = {
    NOTE_E5, NOTE_D5, NOTE_FS4, NOTE_GS4,
    NOTE_CS5, NOTE_B4, NOTE_D4, NOTE_E4,
    NOTE_B4, NOTE_A4, NOTE_CS4, NOTE_E4,
    NOTE_A4};

int durations[] = {
    8, 8, 4, 4,
    8, 8, 4, 4,
    8, 8, 4, 4,
    2};

void setup()
{
  Serial.begin(115200);

  delay(200);

  Serial.println("Serial library initialized.");

 

  pinMode(AIN1, OUTPUT);
  pinMode(AIN2, OUTPUT);
  pinMode(PWMA, OUTPUT);

  pinMode(BIN1, OUTPUT);
  pinMode(BIN2, OUTPUT);
  pinMode(PWMB, OUTPUT);

  pinMode(BUZZER, OUTPUT);



  // IO Expander
  setupExpander(mcp);

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
