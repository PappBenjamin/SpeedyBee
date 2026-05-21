#include "expander.h"

/*
 * Check for interrupts on the IO expander
 @return keypad of the returned interrupt
 @return -1 if no interrupt
 */
int checkExpanderInterrupt()
{
  static int heldPin = -1;

  if (heldPin != -1)
  {
    if (mcp.digitalRead(heldPin) == HIGH)
    {
      heldPin = -1;
    }
    else
    {
      return -1;
    }
  }

  if (!digitalRead(INT_A))
  {
    int x = mcp.getLastInterruptPin();
    mcp.clearInterrupts(); // clear
    if (x >= 0 && x <= 15 && mcp.digitalRead(x) == LOW)
    {
      heldPin = x;
      return x;
    }
  }
  return -1;
}

bool setupExpander()
{
  if (!mcp.begin_I2C(IO_ADDRESS))
  {
    Serial.println("Error.");
    while (1)
      ;
  }

  Serial.println("IO Expander found!");

  pinMode(INT_A, INPUT);

  // IO Expander
  mcp.setupInterrupts(true, false, LOW);

  mcp.pinMode(4, INPUT_PULLUP);
  mcp.pinMode(5, INPUT_PULLUP);
  mcp.pinMode(6, INPUT_PULLUP);
  mcp.pinMode(7, INPUT_PULLUP);

  // enable interrupt on button_pin
  mcp.setupInterruptPin(4, LOW);
  mcp.setupInterruptPin(5, LOW);
  mcp.setupInterruptPin(6, LOW);
  mcp.setupInterruptPin(7, LOW);

  return true;
}