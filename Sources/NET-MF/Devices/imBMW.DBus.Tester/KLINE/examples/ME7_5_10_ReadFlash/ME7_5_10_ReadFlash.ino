//****************hud stuff****************
#include <Wire.h> 
#include <LiquidCrystal_I2C.h>

//includes for K-line and ECU hud
#include <tool.h> 
#include <kline.h>

KLINE Ecu;
TOOL hud;

byte adr=0x01;//Address for target ECU
byte tst=0xF1;//Address for tester

//SD stuff
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
  Ecu.begin(&adr,&tst);
  hud.init();
  pinMode(10, OUTPUT);//CS for SD
  hud.flp("VAG ME7 Flash Demo");
  hud.setCursor(0,1);
  hud.flp("Checking SD...");
  //SD card INIT
  if (!SD.begin(SD_CHIP_SELECT, SPI_FULL_SPEED))
  {
    hud.setCursor(0,3);
    hud.flp("SD Error...");
    while (1){}
  }
  hud.setCursor(14,1);
  hud.flp("Done!");
  delay(1000);
  hud.clear();
}

void run()
{
  hud.clear();
  hud.setCursor(0,0);
  hud.flp("Press any button");
  while (hud.CheckButtonPressed()==0){}
  hud.clear();
  hud.setCursor(0,0);
  if (!start())
  {
    return;
  }
  if (!bypassImmo())
  {
    hud.flp("Error");
    delay(2000);
    return;
  }
  if (!downloadFlash())
  {
    hud.flp("Error");
    delay(2000);
    return;
  }
}
   
boolean start()
{
  hud.flp("Connecting..");
  boolean poop=Ecu.slowInit(0);
  if (!poop)
  {
    hud.clear();
    hud.setCursor(0,0);
    hud.flp("No response");
    delay(2000);
    return 0;
  }
    byte buf[20];
   poop=Ecu.startDiagSession(0x86);
   if (!poop)
   {
     hud.clear();
    hud.setCursor(0,0);
     hud.flp("Diag rejected");
     delay(1000);
     return 0;
   }
   else
   {
     hud.flp("Done!");
     hud.setCursor(0,1);
     return 1;
   }
}

boolean bypassImmo()
{
  hud.flp("Bypass Sec..");
  byte buf[]={0xF0,4};
  if(!Ecu.dynDefDataID(buf,sizeof(buf)))
  {
    return 0;
  }
  byte buf2[]={0xF0,3,1,8,0,0xE2};
  if(!Ecu.dynDefDataID(buf2,sizeof(buf2)))
  {
    return 0;
  }
  byte buf3[20];//to store replies
  if(Ecu.readDataByLocalID(0xF0,buf3)==0)
  {
    return 0;
  }
  byte buf4[]={0,0x30,0xE1,0};
  if(!Ecu.writeMemByAddr(0,0xE2,0x42,4,buf4))
  {
    return 0;
  }
  byte buf5[]={0xF0,4};
  if(!Ecu.dynDefDataID(buf5,sizeof(buf5)))
  {
    return 0;
  }
  hud.flp("Done!");
  hud.setCursor(0,2);
  return 1; 
}
  
  
  

boolean downloadFlash()
{
  hud.setCursor(0,2);
  hud.flp("Reading Flash");
     if (myFile.open("dump.bin", O_WRITE))
     {
       myFile.remove();
       myFile.close();
     }
     if (!myFile.open("dump.bin", O_CREAT | O_WRITE))
     {
       hud.flp("SD error");
       delay(1000);
       return 0;
     }
     long addr=0;//Start of external flash
     byte before=0;
     long cnt=0;
     while (addr < 0x200000)//End of 512kB external flash in RAM
     {
       byte request[]={0x3D,0x38,0x70,0x00,0x30,0x06,0xFC,addr,addr>>8,addr>>16,0x02};
       if((addr&0x00FFFF)==0x3FFC)
       {
         request[6]=4;
       }
       if (!Ecu.write(request,sizeof(request)))
       {
         return 0;
       }
       byte buf[252];
       Ecu.read(buf);
       if (buf[0]!=0x7D)
       {
         return 0;
       }
      byte len=Ecu.readDataByLocalID(0x90,buf);
      if (len==0)
      {
        return 0;
      }
       myFile.write(buf,len);
       if(len>20)
       {
         addr=addr+0xFC;
       }
       else
       {
         addr=addr&0xFF0000;
         addr=addr+0x10000;
       }
       byte percent=(cnt*100)/0x7FFFF;//increased size due to reading 0xFC instead of 0xFF bytes per round
       if (percent != before)
       {
         hud.setCursor(0,3);
         hud.print(percent);
         hud.flp("%");
         before=percent;
       }
       cnt=cnt+len;
     }
     myFile.close();
     Ecu.stopFlashSession();
     hud.setCursor(0,3);
       hud.flp("Done");
       delay(2000);
       return 1;
    
}




void loop()
{
  run();
  
}