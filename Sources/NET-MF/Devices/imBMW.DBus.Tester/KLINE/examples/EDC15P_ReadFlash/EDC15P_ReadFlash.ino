#include <kline.h>
#define flp(string) flashprint(PSTR(string));
#include <Wire.h> 
#include <LiquidCrystal_I2C.h>

LiquidCrystal_I2C lcd(0x27,20,4);
KLINE Ecu;
//This is the push button stuff
const byte OKbuttonPin = 6;
const byte BackbuttonPin = 7;
const byte LeftbuttonPin = 8; 
const byte RightbuttonPin = 9; 

//This is for the menus:
byte optionset=0;
byte mode2=0;
#include <SdFat.h>
#include <SdFatUtil.h>
SdFat SD;
Sd2Card card; 
SdVolume volume; 
const uint8_t SD_CHIP_SELECT = 10;
SdFile myFile;
// store error strings in flash to save RAM 
#define error(s) error_P(PSTR(s))

void setup()                    
{
  pinMode(OKbuttonPin, INPUT);//Set the PB pins
  pinMode(BackbuttonPin, INPUT);
  pinMode(LeftbuttonPin, INPUT);
  pinMode(RightbuttonPin, INPUT);
  lcd.init();
  lcd.backlight();
  lcd.setCursor(0,0);
  flp("EDC15 tool V0.4");
  lcd.setCursor(0,1);
  flp("Checking SD...");
  //SD card INIT
  if (!SD.begin(SD_CHIP_SELECT, SPI_FULL_SPEED))
  {
    lcd.setCursor(0,3);
    flp("SD Error...");
    boolean fail=1;
    while (fail){}
  }
  lcd.setCursor(14,1);
  flp("Done!");
  delay(1000);
}

void run()
{
  lcd.clear();
  lcd.setCursor(0,0);
  flp("Press any button");
  CheckButtonPressed();
  lcd.clear();
  lcd.setCursor(0,0);
  boolean poop=Ecu.fastInit(0x01, 0xF1);
  if (!poop)
  {
    flp("No response");
  }
    byte buf[255];
   if (!Ecu.read(buf))
   {
     flp("CRC Error");
     delay(999999);
   }
   poop=Ecu.startDiagSession(0x01,0x85,buf);
   if (!poop)
   {
     flp("Diag rejected");
     delay(1000);
       return;
   }
   else
   {
     flp("Diag accepted");
     lcd.setCursor(0,1);
     if (!Ecu.securityAccess(0x508DA647,0x3800000,0x41,0x00,buf))
     {
       flp("Security Error");
       delay(1000);
       return;
     }
     else
     {
       flp("Security bypassed!");
     }
     lcd.setCursor(0,2);
     if (!Ecu.requestDownload(0x40,0xE0,0x00,0x00,0x00,0x04,0x20,buf))
     {
       flp("Download rejected");
       delay(1000);
       return;
     }
     else
     {
       flp("Download Accepted");
     }
     lcd.setCursor(0,3);
     if (!myFile.open("EDC15FLD.BIN", O_READ))
     {
       flp("Loader not found");
       delay(1000);
       return;
     }
     int lsize=myFile.fileSize();
     int cnt=0;
     while (cnt<lsize)
     {
       int tmp=lsize-cnt;
       byte buffersize=0;
       
       if (tmp > 0xFE)
       {
         buffersize=0xFE;
         cnt=cnt+0xFE;
       }
       else
       {
         buffersize=tmp;
         cnt=cnt+buffersize;
       }
       byte crip[buffersize];
       myFile.read(crip,buffersize);
       Ecu.sendDownloadData(buffersize,crip);
     }
     myFile.close();//loader sent
     for (byte pp=0;pp<1;pp++)//send some config params
     {
       byte rr[]={0x36,0xC3,0xC3,0x89,0xC5,0x45,0xDA,0x63,0x0B};
       Ecu.write(0,sizeof(rr),rr);
       Ecu.read(rr);
       if (rr[0]!= 0x76)
       {
         flp("Loader Error");
         delay(1000);
       return;
       }
       byte r2[]={0x31,0x02,0x08,0x00,0x00,0x0F,0xBF,0xFF};
       Ecu.write(0,sizeof(r2),r2);
       Ecu.read(r2);
     }
     if (myFile.open("dump.bin", O_WRITE))
     {
       myFile.remove();
       myFile.close();
     }
     if (!myFile.open("dump.bin", O_CREAT | O_WRITE))
     {
       flp("SD error");
       delay(1000);
       return;
     }
     long addr=0x23080000;//Start of external flash
     byte before=0;
     while (addr < 0x230FFFFF)//End of 512kB external flash
     {
       byte request[]={addr>>24,addr>>16,addr>>8,addr,0x00};
       if (addr < 0x230FFF03)
       {
         request[4]=0xFE;
       }
       else
       {
         request[4]=0x2310000-addr;
       }
       addr=addr+request[4];
       Ecu.write(0,sizeof(request),request);
       byte len=Ecu.getUploadData(buf);
       if (len==0)
       {
         lcd.setCursor(0,3);
         flp("Download Error");
         delay(1000);
       return;
       }
       myFile.write(buf,len);
       byte percent=((addr-0x23080000)*100)/0x7FFFF;
       if (percent != before)
       {
         lcd.setCursor(0,3);
         lcd.print(percent);
         flp("%");
         before=percent;
       }
     }
     myFile.close();
     byte prip[]={0xA2,0,0};
     Ecu.write(0,1,prip);
     while(Serial.available()==0){}//Wait for response
     prip[0]=Serial.read();
     //poop=Ecu.StopComms(0x01,buf);
     lcd.setCursor(0,3);
     if (prip[0]==0x55)
     {
       flp("Done");
       delay(1000);
       return;
     }
     else
     {
       flp("Error");
       delay(1000);
       return;
     }
   }
}
   
  


byte CheckButtonPressed()
{
  byte nopress=1;
  byte pushedPB;
  while (nopress==1)
  {
  int read1 = digitalRead(OKbuttonPin);
  int read2 = digitalRead(BackbuttonPin);
  int read3 = digitalRead(LeftbuttonPin);
  int read4 = digitalRead(RightbuttonPin);
    if (read1 == HIGH || read2 == HIGH || read3 == HIGH || read4 == HIGH) 
    {
      // reset the debouncing timer
      delay(100);
      
      if (read1== HIGH)
      {
        
        pushedPB=1;//OK
      }
      if (read2== HIGH)
      {
        pushedPB=2;//Back
        
      }
      if (read3== HIGH)
      {
        pushedPB=3;//Left
      }
      if (read4== HIGH)
      {
        pushedPB=4;//Right
      } 
     nopress=0;
    delay(100);     
    }
  }
  return pushedPB;
}

void flashprint (const char p[])
{
    byte g;
    while (0 != (g = pgm_read_byte(p++))) {
      char j=g;
      lcd.print(j);
    }
}





void loop(){run();}
