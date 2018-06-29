/*
ECU tool Library by Javier Vazquez Vidal

Makes it easier to use the ECU tool hardware

Please read each function to know how to use it

*/

#include <tool.h>
#include <LiquidCrystal_I2C.h>
#include <Wire.h> 
LiquidCrystal_I2C lcd(0x27,20,4);

TOOL::TOOL()
{
  //lcdRow=0;
  //lcdCol=0;
 
}

void TOOL::init()//clears the lcd and sets the cursor in 0,0
{
 pinMode(OKbuttonPin, INPUT);//Set the PB pins
  pinMode(BackbuttonPin, INPUT);
  pinMode(LeftbuttonPin, INPUT);
  pinMode(RightbuttonPin, INPUT);
  pinMode(KLINE_CS,OUTPUT);//33290 CS
  digitalWrite(KLINE_CS,LOW);//KLINE is disabled as default, you need to enable it on startup
  lcd.init();//init the lcd
  lcd.backlight();
}

void TOOL::clear()//clears the lcd and sets the cursor in 0,0
{
  lcd.clear();
  lcd.setCursor(0,0);
}


inline size_t TOOL::write(uint8_t value) {
  lcd.write(value);
  return 1; // assume sucess
}

void TOOL::setCursor(uint8_t a, uint8_t b)//sets the lcd cursor(column,row)
{
  lcd.setCursor(a,b);
  lcdCol=a;
  lcdRow=b;
}

byte TOOL::CheckButtonPressed()//returns which button is pressed. 
{
  byte pushedPB=0;
    if (PIND&0b01000000 || PIND&0b10000000 || PINB&0b00000001 || PINB&0b00000010) 
    {
      if (PIND&0b01000000)
      {        
        return OKbutton;//OK
      }
      if (PIND&0b10000000)
      {
        return Backbutton;//Back 
      }
      if (PINB&0b00000001)
      {
        return Leftbutton;//Left
      }
      if (PINB&0b00000010)
      {
        return Rightbutton;//Right
      } 
    }
  return 0;
}

void TOOL::flashprint (const char p[])
{
//use flp rather than print function for static strings
//as it stores the strings in flash instead of RAM
    byte g;
    while (0 != (g = pgm_read_byte(p++))) {
      char j=g;
      lcd.print(j);
	  /*lcdCol++;//fix for 20x4 lcd scrolling, already included in updated lib
	  if(lcdCol>20)
	  {
	    lcdRow++;
		lcdCol=0;
		if (lcdRow>3)
		{
		  lcdRow=0;
		}
		lcd.setCursor(lcdCol,lcdRow);
	  }*/		
    }
}  

void TOOL::sflashprint (const char p[])
{
//use flp rather than print function for static strings
//as it stores the strings in flash instead of RAM
    byte g;
    while (0 != (g = pgm_read_byte(p++))) {
      char j=g;
      Serial.print(j);
    }
}  