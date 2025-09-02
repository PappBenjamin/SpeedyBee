#include <Arduino.h>


// Buzzer
#include "pitches.h"

// QTR Sensors
#include <QTRSensors.h>


// display
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C

Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);


// IO Expander
#include <Adafruit_MCP23X17.h>
#define IO_ADDRESS 0x22
#define INT_A 26

// Motor pins
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