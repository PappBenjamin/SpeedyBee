#include <Arduino.h>
#include "pitches.h"

//display

#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C

Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

//pins
#define AIN1 6
#define AIN2 7
#define PWMA 8

#define BIN1 20
#define BIN2 21
#define PWMB 22

#define BUZZER 3

#define SCL 5
#define SDA 4


//melody here

int melody[] = {
  NOTE_E5, NOTE_D5, NOTE_FS4, NOTE_GS4, 
  NOTE_CS5, NOTE_B4, NOTE_D4, NOTE_E4, 
  NOTE_B4, NOTE_A4, NOTE_CS4, NOTE_E4,
  NOTE_A4
};

int durations[] = {
  8, 8, 4, 4,
  8, 8, 4, 4,
  8, 8, 4, 4,
  2
};



void setup()
{
  pinMode(AIN1, OUTPUT);
  pinMode(AIN2, OUTPUT);
  pinMode(PWMA, OUTPUT);

  pinMode(BIN1, OUTPUT);
  pinMode(BIN2, OUTPUT);
  pinMode(PWMB, OUTPUT);

  pinMode(BUZZER, OUTPUT);

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


  // display
  Wire.begin();

  if (!display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS))
  {
    Serial1.println("SSD1306 initialization failed!");
    while (true)
      ;
  }

  //draw something
  display.clearDisplay();
  display.setTextSize(2.5);
  display.setTextColor(SSD1306_WHITE);
  display.setCursor(0, 29.5);
  display.println("SpeedyBee!");

  display.display();

}

void loop()
{

  //motor control code here

  //PWM
  analogWrite(PWMA, 50);
  analogWrite(PWMB, 50);

  //move forward
  digitalWrite(AIN1, HIGH);
  digitalWrite(AIN2, LOW);


  digitalWrite(BIN1, HIGH);
  digitalWrite(BIN2, LOW);

  delay(1000);

  //move backward
  digitalWrite(AIN1, LOW);
  digitalWrite(AIN2, HIGH);


  digitalWrite(BIN1, LOW);
  digitalWrite(BIN2, HIGH);

  delay(1000);
}
