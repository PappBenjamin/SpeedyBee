#include <Arduino.h>
#include "pitches.h"

// display
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

// IO Expander
#include <Adafruit_MCP23X17.h>
Adafruit_MCP23X17 mcp;

// IMU
#define BMI323_I2C_ADDRESS 0x68 // Default I²C address when CS is low

// BMI323 register addresses
#define BMI323_CHIP_ID_REG 0x02
#define BMI323_ACCEL_X_LSB 0x12

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C

Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

// IO Expander
#define IO_ADDRESS 0x22
#define INT_A 26

// pins
#define AIN1 6
#define AIN2 7
#define PWMA 8

#define BIN1 20
#define BIN2 21
#define PWMB 22

#define BUZZER 3

#define SCL 5
#define SDA 4

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


#include <QTRSensors.h>

const uint8_t QTRPins[] = {14, 13, 12, 11, 10};
QTRSensors qtr;
int QTRSensorCount = 5;


void setup()
{
  Serial.begin(115200);

  delay(5000);

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

  // IMU
  Wire.beginTransmission(BMI323_I2C_ADDRESS);
  Wire.write(BMI323_CHIP_ID_REG); // Request chip ID register
  Wire.endTransmission();
  Wire.requestFrom(BMI323_I2C_ADDRESS, 1);

  if (Wire.available())
  {
    uint8_t chipID = Wire.read();
    Serial.print("Chip ID: 0x");
    Serial.println(chipID, HEX);
    if (chipID == 0xb0)
    { // Expected chip ID for BMI323
      Serial.println("BMI323 detected!");
    }
    else
    {
      Serial.println("BMI323 not detected. Check connections.");
    }
  }


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

  // Read accelerometer X-axis data
  Wire.beginTransmission(BMI323_I2C_ADDRESS);
  Wire.write(BMI323_ACCEL_X_LSB); // Request X-axis LSB register
  Wire.endTransmission();
  Wire.requestFrom(BMI323_I2C_ADDRESS, 2); // Read 2 bytes (LSB + MSB)

  if (Wire.available() == 2)
  {
    int16_t accelX = Wire.read(); // Read LSB
    accelX |= (Wire.read() << 8); // Read MSB and combine
    Serial.print("Accel X: ");
    Serial.println(accelX);
  }


  u16_t QTRSensorValues[5];
  qtr.read(QTRSensorValues);

  // for (int i = 0; i < QTRSensorCount; i++)
  // {
  //   Serial.print("QTR Sensor ");
  //   Serial.print(i);
  //   Serial.print(": ");
  //   Serial.println(QTRSensorValues[i]);
  // }

  delay(10); // Delay for readability
}
