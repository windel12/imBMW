/*
ISO 9141-2 (K-Line) Library by Javier Vazquez Vidal
javier.vazquez.vidal@gmail.com

Allows communication with any ECU connected to a K-Line bus.

Included Auth algorithms are:

-EDC15
-EDC16

Please read each function to know how to use it

*/



#include "KLINE.h"

byte setspeed=1;//Determines if we have changed, and therefor, if addressing is necessary
byte block=1;//used in KW1281
#define K_IN    0 //RX
#define K_OUT   1 //TX
//#define READEN  2 //read enable for 74HC32 on retail version
#define READEN  17 //read enable for 74HC32 on development version

byte* address;//address of ECU. Remember to change this in your arduino code (address=0x10 for example) every time you want to communicate with a different module!
byte* tester;//tester address. 0xF1 is default, change it if required

KLINE::KLINE()
{
  pinMode(K_OUT, OUTPUT);//Set the modes for the UNO/Mini Pro uart
  pinMode(K_IN, INPUT);
  pinMode(READEN, OUTPUT);
  digitalWrite(READEN,HIGH);
  digitalWrite(K_OUT, HIGH);
}

void KLINE::begin(byte* adr, byte* tst)
{
	/*Used for ECU and tester address pointer setup*/
  tester=tst;
  address=adr;
}



//******************Seed/Key operations**********//


boolean KLINE::securityAccess(long Key, byte accmod)
{
	/*long key: It is the key to be used to process the algorithm, and it is unique to each EDC15/EDC16 variant.
	accmod: Access mode (Security level to be accessed)
	*/
  if (accmod == 0x01)
  {
	return LVL1Auth(Key, accmod);
  }
  else if (accmod == 0x03)
  {
	return LVL3Auth(Key, accmod);
  }
  else if(accmod==0x67)
  {
	return LVL67Auth(Key, accmod);
  }
  else if(accmod==0x30)
  {
	return LVL30Auth(Key, accmod);
  }
  else
  
  {
	return 0;
  }
}

boolean KLINE::securityAccess(long Key, long Key2, byte accmod, byte accparam)
{
	/*long key: It is the key to be used to process the algorithm, and it is unique to each EDC15/EDC16 variant.
	accmod: Access mode (Security level to be accessed)
	accparam: Access parameter (Used only on EDC15)
	*/
  if (accmod == 0x41)
  
  {
	return LVL41Auth(Key, Key2, accmod, accparam);
  }
  else return 0;
}

  
boolean KLINE::LVL3Auth(long Key, byte accmod)
{
 byte buffer[6];
 if (!RequestSeed(accmod, buffer))
  {
	return 0;
  }
  //now we handle the seed bytes 
  long Keyread=0;
  Keyread = buffer[2];
  Keyread = Keyread<<8;
  Keyread = Keyread+buffer[3];
  Keyread = Keyread<<8;
  Keyread = Keyread+buffer[4];
  Keyread = Keyread<<8;
  Keyread = Keyread+buffer[5];
  Keyread=Keyread+Key;//here is where the key is used
 //Done with the key generation 
  
  if (!SendKey(Keyread, accmod))
  {
    return 0;
  }
  return 1;
}


//boolean KLINE::LVL1Auth(long Key, long Magic1, byte accmod)
boolean KLINE::LVL1Auth(long Key, byte accmod)
{
  byte buffer[6];
  /*int Key2=Key;
  Key=Key>>16;
  int Key1=Key;*/
  if (!RequestSeed(accmod, buffer))
  {
	return 0;
  }
  //now we handle the seed bytes 
  long Keyread1=buffer[2];
  Keyread1=(Keyread1<<8)+buffer[3];
  Keyread1=(Keyread1<<8)+buffer[4];
  Keyread1=(Keyread1<<8)+buffer[5];
  /*long tempstring; //kept for historical reference
  tempstring = buffer[2];
  tempstring = tempstring<<8;
  long Keyread1 = tempstring+buffer[3];
  tempstring = buffer[4];
  tempstring = tempstring<<8;
  long Keyread2 = tempstring+buffer[5];
  byte counter=0;
  while (counter<5)//kept for historical reference
    {
     long temp1;
     tempstring = Keyread1;
     tempstring = tempstring&0x8000;
     Keyread1 = Keyread1 << 1;
     temp1=tempstring&0xFFFF;//Same as EDC15 until this point
      if (temp1 == 0)//this part is the same for EDC15 and EDC16
       {
          long temp2 = Keyread2&0xFFFF;
          long temp3 = tempstring&0xFFFF0000;
          tempstring = temp2+temp3;
          Keyread1 = Keyread1&0xFFFE;
          temp2 = tempstring&0xFFFF;
          temp2 = temp2 >> 0x0F;
          tempstring = tempstring&0xFFFF0000;
          tempstring = tempstring+temp2;
          Keyread1 = Keyread1|tempstring;
          Keyread2 = Keyread2 << 1;
       }

     else
      { 
         long temp2;
         long temp3;
         tempstring = Keyread2+Keyread2;
         Keyread1 = Keyread1&0xFFFE;
         temp2=tempstring&0xFF;//Same as EDC15 until this point
         temp3=Magic1&0xFFFFFF00;
         temp2= temp2|1;
         Magic1=temp2+temp3;
         Magic1 = Magic1&0xFFFF00FF;
         Magic1 = Magic1|tempstring;
         temp2=Keyread2&0xFFFF;
         temp3=tempstring&0xFFFF0000;
         temp2=temp2 >> 0x0F;
         tempstring=temp2+temp3;
         tempstring=tempstring|Keyread1;
         Magic1=Magic1^Key1;
         tempstring=tempstring^Key2;
         Keyread2=Magic1;
         Keyread1=tempstring;
      }
  
  counter++;
 }

//Done with the key generation 
  Keyread2=Keyread2&0xFFFF;//Clean first and secong word from garbage
  Keyread1=Keyread1&0xFFFF;
  */
  int cnt=0;//new algo, much cleaner! wow!
  for (cnt=0;cnt<5;cnt++)
  {
    long tmp=Keyread1;
	Keyread1=Keyread1<<1;
    if ((tmp&0x80000000) != 0)
    {
      Keyread1=Keyread1^Key;
      Keyread1=Keyread1&0xFFFFFFFE;
    }
  }
  long Keyread2=Keyread1&0x0000FFFF;
  Keyread1=Keyread1>>16;
  
  if (!SendKey(Keyread1,Keyread2, accmod, buffer))
  {
    return 0;
  }
  return 1;
}


	
boolean KLINE::LVL41Auth(long Key, long Key3, byte accmod, byte accparam)
{	
  byte buf[6];
  if (!RequestSeed(accmod, accparam, buf))
  {
	return 0;
  }
  if (buf[2]==0 && buf[3]==0) //VAG EDC15 bug, key calculation not required!
  {
	  if (!SendKey(0x0,0x0, accmod, buf))
	  {
		return 0;
	  }
	return 1;
  }
  long Key1;
  long Key2;
  //long Key3 = 0x3800000;
  long tempstring;
  tempstring = buf[2];
  tempstring = tempstring<<8;
  long Keyread1 = tempstring+buf[3];
  tempstring = buf[4];
  tempstring = tempstring<<8;
  long Keyread2 = tempstring+buf[5];
//Process the algorithm 
   Key2=Key;
   Key2=Key2&0xFFFF;
   Key=Key>>16;
   Key1=Key;
  for (byte counter=0;counter<5;counter++)
    {
       long temp1;
     long KeyTemp = Keyread1;
     KeyTemp = KeyTemp&0x8000;
     Keyread1 = Keyread1 << 1;
     temp1=KeyTemp&0x0FFFF;
     if (temp1 == 0)
      {
       long temp2 = Keyread2&0xFFFF;
       long temp3 = KeyTemp&0xFFFF0000;
        KeyTemp = temp2+temp3;
        Keyread1 = Keyread1&0xFFFE;
        temp2 = KeyTemp&0xFFFF;
        temp2 = temp2 >> 0x0F;
        KeyTemp = KeyTemp&0xFFFF0000;
        KeyTemp = KeyTemp+temp2;
        Keyread1 = Keyread1|KeyTemp;
        Keyread2 = Keyread2 << 0x01;
      }

   else

    { 
       long temp2;
       long temp3;
       KeyTemp = Keyread2+Keyread2;
       Keyread1 = Keyread1&0xFFFE;
       temp2=KeyTemp&0xFF;
       temp2= temp2|1;
       temp3=Key3&0xFFFFFF00;
       Key3 = temp2+temp3;
       Key3 = Key3&0xFFFF00FF;
       Key3 = Key3|KeyTemp;
       temp2 = Keyread2&0xFFFF;
       temp3 = KeyTemp&0xFFFF0000;
       KeyTemp = temp2+temp3;
       temp2 = KeyTemp&0xFFFF;
       temp2 = temp2 >> 0x0F;
       KeyTemp = KeyTemp&0xFFFF0000;
       KeyTemp = KeyTemp+temp2;
       KeyTemp = KeyTemp|Keyread1;
       Key3 = Key1^Key3;
       KeyTemp = Key2^KeyTemp;
       Keyread2 = Key3;
       Keyread1 = KeyTemp;
      }
    }
//Done with the key generation 
  Keyread2=Keyread2&0xFFFF;//Clean first and secong word from garbage
  Keyread1=Keyread1&0xFFFF;
  if (!SendKey(Keyread1,Keyread2, accmod, buf))
  {
    return 0;
  }
  return 1;
}

boolean KLINE::LVL67Auth(long Key, byte accmod)
{
byte buffer[6];
byte accparam=random(255);//Just a random number, part of the algo used as counter
if (!RequestSeed(accmod, accparam, buffer))
  {
	return 0;
  }
  delay(15);//algo is so fast that need to add manual delay
long Keyread1=buffer[2];
  //now we handle the seed bytes 
  for (byte r=3;r<6;r++)
  {
    Keyread1=Keyread1<<8;
    Keyread1=Keyread1+buffer[r];
  }
  long Keyread2;
  accparam=accparam+0x23;
  while(accparam!=0)
  {
    Keyread2=Keyread1;
    Keyread2=Keyread2>>0x1F;
    byte p=Keyread2;
    p=p&1;
    Keyread1=Keyread1<<1;
    if (p!=0)
    {
      Keyread1=Keyread1^Key;//here is where we use the key
    }
    accparam--;
  }
  //Done with key generation
  if (!SendKey(Keyread1, accmod))
  {
    return 0;
  }
  return 1;
}

boolean KLINE::LVL30Auth(long Key, byte accmod)
{
byte buffer[6];
byte accparam=random(255);//Just a random number, part of the algo used as counter
if (!RequestSeed(accmod, accparam, buffer))
  {
	return 0;
  }
  delay(15);//algo is so fast that need to add manual delay
  //now we handle the seed bytes 
long Keyread1=buffer[2];
  //now we handle the seed bytes 
  for (byte r=3;r<6;r++)
  {
    Keyread1=Keyread1<<8;
    Keyread1=Keyread1+buffer[r];
  }
  long Keyread2;
  accparam=accparam+0x23;
  while(accparam!=0)
  {
    Keyread2=Keyread1;
    Keyread2=Keyread2&0x80000000;
    long p=Keyread2;
    Keyread1=Keyread1<<1;
    if (p!=0)
    {
      Keyread1=Keyread1^Key;//here is where we use the key
    }
    accparam--;
  }
  //Done with key generation
  if (!SendKey(Keyread1, accmod))
  {
    return 0;
  }
  return 1;
}




boolean KLINE::RequestSeed(byte accmod, byte accparam,byte buffer[])
{
  byte data[]={0x27,accmod,accparam};
  write(data,3);
  read(buffer);
  if (buffer[0]==0x67)
  {
	return 1;
  }
  return 0;
}

boolean KLINE::RequestSeed(byte accmod, byte buffer[])
{
  byte data[]={0x27,accmod};
  write(data,2);
  read(buffer);
  if (buffer[0]==0x67)
  {
	return 1;
  }
  return 0;
}

boolean KLINE::SendKey(int Key1, int Key2, byte inc, byte buffer[])//Requires the generated keys and the number to be incremented
{
  buffer[0]=0x27;
  buffer[1]=inc+1;
  buffer[3]=Key1;
  Key1=Key1>>8;
  buffer[2]=Key1;
  buffer[5]=Key2;
  Key2=Key2>>8;
  buffer[4]=Key2;
  write(buffer,6);
  read(buffer);
  if (buffer[0] == 0x7F)//if auth failed...
  {
    return 0;
  }
  return 1;
}

boolean KLINE::SendKey(long Key, byte inc)//Requires the generated keys and the number to be incremented
{
  byte buffer[6]={0x27,inc+1,Key>>24,Key>>16,Key>>8,Key};
  write(buffer,6);
  read(buffer);
  if (buffer[0] == 0x7F)//if auth failed...
  {
    return 0;
  }
  return 1;
}

//************************Wake up operations*****************/////////////  
  
  
  void KLINE::bitBang()
{
	digitalWrite(READEN,HIGH);
	serial_tx_off(); 
	serial_rx_off();
	digitalWrite(K_OUT, HIGH);
	delay(300);
	digitalWrite(K_OUT, LOW);
	delay(200);
	byte mask=1;
	for (mask = 00000001; mask>0; mask <<= 1)  //iterate through bit mask
	 {
		 if (*address & mask) // if bitwise AND resolves to true
		 {
			 digitalWrite(K_OUT, HIGH);
		 }
		 else
		 {
			 digitalWrite(K_OUT, LOW);
		 }
		 delay(200);
	 }
	digitalWrite(K_OUT, HIGH);
	Serial.begin(9600);
	delay(3);
	digitalWrite(READEN,LOW);
}




byte KLINE::slowInit(byte skip) //skip can be 0 (dont skip) or 1. It is used to skip comms after we get the response from ecu to the 5 baud init
 {
	/*Makes a slow init to the target address and then switches to the selected baudrate*/
  byte cnt=0;
  block=1;
  while (cnt <3)
  {
	bitBang();
	int delayy=0;
	while(Serial.available()<1 && delayy < 2000)
	{
		delay(1);
		delayy++;
	}
	if (Serial.available()==1)
	{
		byte b=0;
		b=Serial.read();
		if(b==0x55){}//its ok, do nothing
		else
		{
			Serial.begin(10400);//correct baudrate!
		}
		while(Serial.available()<2){}//wait for the other bytes
		for (byte j=0;j<2;j++)
		{
			b=Serial.read();
		}
		delay(44);
		if (skip==1)
		{
			return 1;
		}
		digitalWrite(READEN,HIGH);
		Serial.write(~b);
		while (!(UCSR0A & _BV(TXC0)));//wait for data to be sent
		digitalWrite(READEN,LOW);
		while(Serial.available()<1){}//wait for reply
		b=Serial.read();
		byte u=~*address;//compiler bullshit, doesnt work directly
		if (b==u)//Inverse of the target address means its ready to communicate
		{
			setspeed=1;
			delay(75);
			return 1;
		}
		else//kw1281 attacks!
		{
			return b;//all ok BUT KW1281 session, so no fast mode yet. returns size of pending packet
		}
	}
	cnt++;
  }
  return 0;
}

void KLINE::closeKW1281Session()//Self explanatory
{
  byte cls[]={0x06};
  writeKW1281(cls,1);
}

byte KLINE::readKW1281(byte cnt, byte buf[])//block counter is stored on the first byte of the array. It returns the total array size
{
	writeByte(~cnt);
	byte b=0;//junkie byte!
	for(byte y=0;y<2;y++)
	{
		while(Serial.available()<1){}//wait to get the byte
		b=Serial.read();//group stuff and block counter
		writeByte(~b);
	}
	for (byte cnt1=0;cnt1<cnt-3;cnt1++)
	{
		while(Serial.available()<1){}//wait to get the byte
		buf[cnt1]=Serial.read();
		writeByte(~buf[cnt1]);
	}
	while(Serial.available()<1){}//wait to get the byte
	b=Serial.read();//assumed 0x03
	if (b!=0x03)
	{
		return 0;//this should never happen, so if it happens, please report the bug!
	}
	block++;
	return (cnt-3);//returns the size
}

byte KLINE::readKW1281(byte buf[])//block counter is stored on the first byte of the array. It returns the total array size
{
	while(Serial.available()<1){}//wait to get the byte
	byte cnt=Serial.read();//cnt for frame length
	writeByte(~cnt);
	byte b=0;//junkie byte!
	for(byte y=0;y<2;y++)
	{
		while(Serial.available()<1){}//wait to get the byte
		b=Serial.read();//group stuff and block counter
		writeByte(~b);
	}
	for (byte cnt1=0;cnt1<cnt-3;cnt1++)
	{
		while(Serial.available()<1){}//wait to get the byte
		buf[cnt1]=Serial.read();
		writeByte(~buf[cnt1]);
	}
	while(Serial.available()<1){}//wait to get the byte
	b=Serial.read();//assumed 0x03
	if (b!=0x03)
	{
		return 0;//this should never happen, so if it happens, please report the bug!
	}
	block++;
	return (cnt-3);//returns the size
}

void KLINE::writeKW1281(byte buf[], byte len)//block counter is stored on the first byte of the array. It returns the total array size
{
  writeByte(len+2);//send the block size
  while(Serial.available()<1);
  byte b=Serial.read();
  writeByte(block);//send the block count
  block++;
  while(Serial.available()<1);
  b=Serial.read();
  for (byte q=0;q<len;q++)//send the buffer
  {
	writeByte(buf[q]);
	while(Serial.available()<1){}//wait for the reply
	b=Serial.read();
  }
  writeByte(0x03);//send the block termination byte
	
}


boolean KLINE::writeByte(byte b)
{
	delay(10);
	digitalWrite(READEN,HIGH);
	Serial.write(b);
	while (!(UCSR0A & _BV(TXC0)));//wait for data to be sent
	digitalWrite(READEN,LOW);
}
  
boolean KLINE::fastInit()
{
  byte cnt=0;
  setspeed=1;
  while (Serial.available()==0 && cnt < 10)
  {
    digitalWrite(READEN,HIGH);
	serial_tx_off(); //disable UART so we can "bit-Bang" the fast init.
    digitalWrite(K_OUT, LOW);
    delay(26);
    digitalWrite(K_OUT, HIGH);
    Serial.begin(10400);
    delay(25);
    byte buf[5]={0x81,*address,*tester,0x81,0x0};//we must send the packet manually since the retry system would mess here
    buf[4]=iso_checksum(buf,4);
	Serial.write(buf[0]);
    delay(13);
	Serial.write(buf[1]);
	delay(13);
    Serial.write(buf[2]);
	delay(13);
    Serial.write(buf[3]);
	delay(13);
    Serial.write(buf[4]); 
	while (!(UCSR0A & _BV(TXC0)));//wait for data to be sent
	digitalWrite(READEN,LOW);
    int counter=0;
    while (counter < 1000 && Serial.available()==0)
    {
      delay(1);
      counter++;
    }
    cnt++;
  }
  
  if (cnt < 10 )
  {
    return 1;//if we got an answer from the ECU, we will return true
  }
  
  else
  
  {
    return 0;
  }
}


 
///////************Port Handling********/////////  
void KLINE::serial_rx_off()
{
  UCSR0B &= ~(_BV(RXEN0));  //disable UART RX
}

void KLINE::serial_tx_off() 
{
   UCSR0B &= ~(_BV(TXEN0));  //disable UART TX
   digitalWrite(K_OUT,HIGH);
}  
  


void KLINE::send_delay(byte del)//Indicates the delay between bytes for data sent
{
  if (setspeed==1 || setspeed==0)
  {
	delay(del);// ISO requires 5-20 ms delay between bytes.
  }
  else
  {
    delayMicroseconds(3);
  }
  
}

//*********Requests/commands*********//
  
boolean KLINE::startComms(byte buffer[]) //Start communications packet
{
   setspeed=1;
   byte data[]={0x81};
   boolean reply=write(data,1);
   if (!reply)
   {
     return 0;
   }
   read(buffer);
   return 1;
}


boolean KLINE::stopComms()//Stop communications packet, fast mode
{
   byte data[3]={0x82};
   write(data,1);
   read(data);
   if (data[0]==0xC2)//If the ECU acks the stop communications...
   {
		return 1;
	}
   return 0;
}

boolean KLINE::testerPresent() //Tester present packet
{
   byte data[3]={0x3E};
   write(data,1);
   if (!write(data,1))
   {
     return 0;
   }
   read(data);
   if (data[0]==0x7E)
   {
	 return 1;
   }
	return 0;
}

boolean KLINE::reset(byte type, byte buffer[]) //ECU reset packet, NOT supported by EDC15
{
	/*
	Types:
	0x01: Hard Reset 
    0x02: Key Off On Reset 
    0x03: Soft Reset 
	*/
   byte data[]={0x11, type};
   write(data,2);
   read(buffer);
   if (buffer[0] != 0x7F)
   {
	return 1;
   }
   return 0;
}

boolean KLINE::clearEmissionsDTCs() //Clear DTCs packet
{
/*This will wipe the following data:

- MIL and number of diagnostic trouble codes
- Clear the I/M (Inspection/Maintenance) readiness bits
- Confirmed Emissions diagnostic trouble codes
- Pending diagnostic trouble codes
- Diagnostic trouble code for freeze frame data
- Freeze frame data 
- Oxygen sensor test data (Be careful if you are going to inspection soon!. It takes about one week to get this again!)
- Status of system monitoring tests
- On-board monitoring test results
- Distance travelled while MIL is activated 
- Number of warm-ups since DTCs cleared 
- Distance travelled since DTCs cleared 
- Time run by the engine while MIL is activated 
- Time since diagnostic trouble codes cleared 
*/

   byte data1[3]={4};//Clear emission related faults
   if (!PIDwrite(data1,1))
   {
	 return 0;
   }
   PIDread(data1);
	return 1;//dunno how to check if it was done, so for now, its always done!

}

boolean KLINE::clearGeneralDTCs() //Clear DTCs packet in fast mode
{
   byte data1[4]={0x14,0xFF,0xFF,0xFF};//Clear general faults
   write(data1,4);
   read(data1);
   if (data1[0]==0x54)
   {
	return 1;
   }
   return 0;
}




byte KLINE::readECUID(byte opt, byte buffer[])//Retrieves ECU info in fast mode, returns the size of the reply
{
	//You need to start a Standard diag session before using it!
	/*The following options are available:         EDC15/EDC16-17
											0x80 - All data
											0x86 - VIN and Hardware no.
											0x8A - Boot version
											0x90 - VIN
											0x91 - Manufacturer Drawing number / Main Firmware version 
											0x92 - ECU hardware no.
											0x93 - ECU hardware version no.
											0x94 - ECU software no.
											0x95 - ECU software version no.
											0x96 - Homologation code
											0x97 - ISO code
											0x98 - Tester code
											0x99 - Reprogramming/production date [Y-Y-M-D]
											0x9B - NA/Configuration software version
											*/
  byte data[]={0x1A,opt};
  boolean reply=write(data,2); 
  if (!reply)
   {
     return 0;
   }
   return read(buffer);
}


boolean KLINE::accessTiming(byte buffer[],byte cmd,byte p2min, byte p2max, byte p3min, byte p3max, byte p4min)//fast mode
{
  /*cmd options:
      -00: read possible limits
      -01:reset to default values
      -02:read current values
      -03:set values
      
    P2 config: 15<=P2<=50 ms (number is in ms) - time between ECU reply and tester next request
    P3 config: 0.5<=P3<=5000 ms (number is in ms) -time between tester request and ecu reply
    P4 config: 0<=P4<=20 ms (number is in ms) - time between ECU bytes
    pXmin= minimum
    pXmax= maximum
    */
    byte data[]={0x83,cmd,p2min,p2max,p3min,p3max,p4min};
    boolean reply=write(data,7);
    if (!reply)
    {
      return 0;
    }
    read(buffer);
    return 1;
}



byte KLINE::requestDownload(byte memaddrh, byte memaddrm, byte memaddrl, byte dataformID, byte ucmsh, byte ucmsm, byte ucmsl)
{
	/*  Requests download to the ECU (write ECU flash), returns the data length accepted by ECU
		
		Memory address to start writing at
		memaddrh: Memory address high byte
		memaddrm: Memory address middle byte
		memaddrl: Memory address low byte
		
		dataformID: Data format identifier (00 for no encryption and uncompressed used on EDC15, 0x02 for EDC16 with Encryption)
		
		This indicates the size of the file to be downloaded:
		ucmsh: Uncompressed memory size high byte
		ucmsm: Uncompressed memory size middle byte
		ucmsl: Uncompressed memory size lower byte
		*/
		byte data[]={0x34,memaddrh, memaddrm, memaddrl, dataformID, ucmsh, ucmsm, ucmsl};
		write(data,sizeof(data));
		read(data);
		if (data[0]==0x74)
		{
			return data[1];
		}
		return 0;
}

boolean KLINE::requestUpload(byte memaddrh, byte memaddrm, byte memaddrl, byte dataformID, byte ucmsh, byte ucmsm, byte ucmsl)
{
	/*  Requests upload from the ECU (read ECU flash)
		
		Memory address to start reading at
		memaddrh: Memory address high byte
		memaddrm: Memory address middle byte
		memaddrl: Memory address low byte
		
		dataformID: Data format identifier 
					
					First digit indicates compression, second digit indicates encryption.
					0x00 means no encryption and uncompressed
		
		This indicates the size of the file to be uploaded:
		ucmsh: Uncompressed memory size high byte
		ucmsm: Uncompressed memory size middle byte
		ucmsl: Uncompressed memory size lower byte
		*/
		byte data[]={0x35,memaddrh, memaddrm, memaddrl, dataformID, ucmsh, ucmsm, ucmsl};
		write(data,sizeof(data));
		read(data);
		if (data[0]==0x75)
		{
			return 1;
		}
		return 0;
}

boolean KLINE::requestTransferData(byte buffer[]) //Transfer data packet, EDC16 version
{
	/*Requests a data packet from ECU while in Upload service, and returns the read data*/
   byte data[]={0x36};
   if (write(data,1)==0)
   {
	return 0;
   }
   return readUploadData(buffer);
}

boolean KLINE::transferData(long addr, byte buffer[], byte len) //Transfer data packet, EDC15 version
{
	/*Sends a packet to ECU while in Download service*/
   byte tmp[]={0x36,addr>>24,addr>>16,addr>>8,addr};
   write(tmp,5,buffer,len);
   read(buffer);
   if(buffer[0]==0x76)
   {
	return 1;
	}
	return 0;
   
}

boolean KLINE::transferData24(long addr, byte buffer[], byte len) //Transfer data packet, EDC15C version for 24bit address
{
	/*Sends a packet to ECU while in Download service*/
   byte tmp[]={0x36,addr>>16,addr>>8,addr};
   write(tmp,4,buffer,len);
   read(buffer);
   if(buffer[0]==0x76)
   {
	return 1;
	}
	return 0;
   
}

byte KLINE::readUploadData(byte buffer[])
{
	while (Serial.available() < 3){}//wait to get the header...
	byte len=Serial.read();
	if (len==0x00)
	{
		len=Serial.read();
	}
	byte chk=len+Serial.read();//We start storing data for the checksum
	for (byte cnt=0; cnt < len-1; cnt++)
	{
		while (Serial.available()==0){}//wait until we get the byte
		buffer[cnt]=Serial.read();
	}
	while (Serial.available()==0){}//wait until we get the checksum
	byte chk2=Serial.read();
	chk=chk+iso_checksum(buffer,len-1);
	if (chk != chk2)
	{
		return 0;
	}
	return (len-1);
}
	

boolean KLINE::startDiagSession(byte sub)
{
  /*sub must be 0x85 to write (download) and 0x86 to read (upload) the flash
	Possible Modes are:
								0x81 - Standard Diagnostics
								0x84 - End of Line supplier mode
								0x85 - Download mode
								0x86 - Upload mode
								0x89 - Yet another Standard Diag session
								*/
  //We try all possible baudrates to get the fastest link speed possible
  //first we try to init diag normally:
  byte pata[]={0x10,sub,0};
  write(pata,2);
  read(pata);
  if (pata[0]!=0x50)
  {
	return 0;
  }
  setspeed=2;
  //we dont care what the reply was like anyway, so lets get started with speed...
  /*byte data[]={0x10,sub,0xA7};//This baudrate seems unstable on some ECUS, will discard it until a solution is found
  write(data,sizeof(data));
  read(data);
  if (data[0] == 0x50)//If diag session is accepted...
    {
	  setspeed=2;
	  Serial.begin(250000);//set the baudrate for the com port
      return 1;
    }*/
  byte data1[]={0x10,sub,0x87};
  write(data1,3);
  read(data1);
  if (data1[0] == 0x50)
    {
      setspeed=2;
	  Serial.begin(125000);
      return 1;
    }
  byte data2[]={0x10,sub,0x74};
  write(data2,3);
  read(data2);
  if (data2[0] == 0x50)
    {
      setspeed=2;
	  Serial.begin(83500);
      return 1;
    }
	
  byte data3[]={0x10,sub,0x66};
  write(data3,3);
  read(data3);
  if (data3[0] == 0x50)
    {
      setspeed=2;
	  Serial.begin(63500);
      return 1;
    }

  byte data4[]={0x10,sub,0x50};
  write(data4,3);
  read(data4);
  if (data4[0] == 0x50)
    {
      setspeed=2;
	  Serial.begin(38400);
      return 1;
    }		
   return 1;//minimum speed, but at least we still got it
}  
  
boolean KLINE::stopDiagSession() //Stop diagnostics session packet
{
   byte data[2]={0x20,0};
   write(data,1);
   read(data);
   if (data[0]==0x60)
   {
	return 1;
   }
   return 0;
}

boolean KLINE::stopFlashSession() //EDC15, terminate session after read/write flash
{
   byte data[]={0xA2};
   write(data,1);
   byte cnt=0;
   while(Serial.available()<1 && cnt<200)
   {
    delay(1);
	cnt++;
   }
   if(cnt==200)
   {
    return 0;
   }
   byte q=Serial.read();
   if (q==0x55)
   {
	return 1;
   }
   return 0;
}

byte KLINE::getPID(byte PID, byte buffer[]) //Get PID data and put it in an array
{
	//be careful to use a big enough buffer!!
   byte data[30]={1,PID};//Mode 1 to get the PID data
   if (!PIDwrite(data,2))
   {
	 return 0;
   }
   return PIDread(buffer);
}


long KLINE::getPIDSupport(byte PID) //Get Supported PIDs
{
   byte data[4]={1,PID,0,0};//Mode 1 to get the PID data
   if (!PIDwrite(data,2))
   {
	 return 0;
   }
   PIDread(data);
   long resultt=data[0];
   resultt=(resultt<<8)+data[1];
   resultt=(resultt<<8)+data[2];
   resultt=(resultt<<8)+data[3];
   return (resultt);

}

byte KLINE::getEmissionsDTCs(byte buffer[]) //Get DTC data and put it in an array
{
	//be careful to use a big enough buffer!!
   byte data[10]={3};//Mode 3 to get emissions DTC data
   if (!PIDwrite(data,1))
   {
	 return 0;
   }
   return PIDread(buffer);
}

byte KLINE::getDTCs(byte buffer[]) //Get DTC data and put it in an array
{
	//be careful to use a big enough buffer!!
   byte data[10]={18};//Mode 18 to get DTC data
   write(data,1);
   byte len=read(data);
   if (data[0]!=0x58)//if we did not get a good reply
   {
	return 0;
   }
   if (data[1]!=0)//No DTCs!
   {
	return 1;
   }
   int cnt=0;
	while(len!=0)
	{
	  for(byte r=1;r<len;r=r+2)//jumps of two for each dtc
	  {
		if(data[r] !=0 && data[r+1] != 0)//we skip the first byte
		{
			buffer[cnt]=data[r];
			cnt++;
			buffer[cnt]=data[r+1];
			cnt++;
		}
	  }
	  len=read(data);
	}
   return (cnt);
}

boolean KLINE::dynDefDataID(byte buffer[], byte len) //Dynamically define data ID packet
{
   byte tmp[]={0x2C};
   write(tmp,1,buffer,len);
   read(buffer);
   if (buffer[0]==0x6C)
   {
	return 1;
   }
   return 0;
}
 
byte KLINE::readDataByLocalID(byte ID, byte buffer[]) //Reads a local ID block
{
   //returns the length of the reply
   byte data[]={0x21,ID};
   write(data,2);
   return getIDData(buffer);
   
}

boolean KLINE::writeDataByLocalID(byte ID, byte buffer[], byte len) //Writes a local ID block
{
   byte tmp[]={0x3B,ID};
   write(tmp,2,buffer,len);
   read(buffer);
   if (buffer[0]==0x7B)
   {
	return 1;
   }
   return 0;
}

boolean KLINE::writeMemByAddr(byte hiMem, byte midMem, byte lowMem, byte length, byte buffer[]) //Reads a local ID block
{
   /* Writes a data string to a memory address
   
		-byte hiMem: Higher byte of mem address
		-byte midMem: Middle byte of mem address
		-byte lowMem: lower byte of mem address
		-byte length: total length of the data (just data)
		-byte buffer[]: the data to be written
		*/
     
   byte tmp[]={0x3D,hiMem,midMem,lowMem,length};
   write(tmp,sizeof(tmp),buffer,length);
   read(buffer);
   if (buffer[0]==0x7D)
   {
	return 1;
   }
   return 0;
}

byte KLINE::readMemByAddr(byte hiMem, byte midMem, byte lowMem, byte length, byte buffer[]) //Reads a local ID block
{
   /* Reads a data string from a memory address
   
		-byte hiMem: Higher byte of mem address
		-byte midMem: Middle byte of mem address
		-byte lowMem: lower byte of mem address
		-byte length: total length of the data (just data)
		-byte buffer[]: destination of read data, must be at least same size as the value specified in "length"
		*/
     
   byte data[]={0x23,hiMem,midMem,lowMem,length};
   write(data,sizeof(data));
   byte len=getUploadData(buffer);
   return len;
}
 
boolean KLINE::startRoutine(byte buffer[], byte len) //Start Routine by Local ID packet
{
	/* To use this function, we must pass an array with all setings.
	The first byte of the array indicates the routine to start, and all the others indicate
	the parameters for this routine
	*/
   byte tmp[]={0x31};
   write(tmp,sizeof(tmp),buffer,len);
   read(buffer);
   if (buffer[0]==0x71)
   {
	return 1;
   }
   return 0;
} 

boolean KLINE::requestRoutineResults(byte ID, byte buffer[])
{
	/* Requests the results for a routine previously started with StartRoutine*/
   byte data[]={0x33, ID};
   write(data,2);
   read(buffer);
   if (buffer[0]==0x73)
   {
	return 1;
   }
   return 0;
}  

boolean KLINE::requestTransferExit()
{
	/* Requests end of file transfer after reading or writing a memory block*/
   byte data[4]={0x37};
   write(data,1);
   read(data);
   if (data[0]==0x77)
   {
	return 1;
   }
   return 0;
}  
  
  
 //************read/write a string or loader*************// 
 
 byte KLINE::PIDread(byte buffer[])//reads incoming data and returns only the data
{  
	//Reads a string, writes only the data on the buffer, and returns the length of the data.
	int cnt=0;
	byte chk=0;
	while(Serial.available()<5){}//now wait for the whole header
	for (byte cnt=0; cnt < 5; cnt++)
    {
		chk=chk+Serial.read();
	}
	cnt=0;//reset the counter
	byte cnt2=0;
    while(cnt<10)//Poopy way to get the whole frame, due to the poor DTC protocol specs, as YOU are supposed to know the packet length
    {
	  if (Serial.available()>0)
	  {
		cnt=0;
        buffer[cnt2]=Serial.read();
		chk=chk+buffer[cnt2];
		cnt2++;
	  }
	  delay(1);
	  cnt++;
    }
	cnt2--;
	chk=chk-buffer[cnt2];//we added the checksum byte too, now we need to remove it.
    if (chk != buffer[cnt2])
    {
      return 0;//checksum error
    }
	delay(40);
    return cnt2;
}
  
byte KLINE::read(byte buffer[])//reads incoming data and returns only the data
{  
	//Reads a string, writes only the data on the buffer, and returns the length of the data.
	int cnt=0;
	while(Serial.available()<3 && cnt<1000)
	{
     delay(1);
	 cnt++;
	}//wait 1 second or to have the header with the minimum string length!!!
	if(cnt==1000)
	{
	  return 0;//if there was no data
	}
	byte len=Serial.read();
    byte chk=len;
	if (len==0)
	{
	  len=Serial.read();
	}
	if (setspeed==1 || setspeed==0)
    {
      len=len-0x80;
      byte dest=Serial.read();
      byte orig=Serial.read();
      chk=chk+dest+orig;
    }
    for (int cnt=0; cnt < len; cnt++)
    {
	  while(Serial.available()==0){}//wait for the next byte
      buffer[cnt]=Serial.read();
    }
	while(Serial.available()==0){}//wait for the checksumm
    byte chk2=Serial.read();
    chk=chk+iso_checksum(buffer,len);
    if (chk != chk2)
    {
      return 0;//checksum error
    }
    while (buffer[0]==0x7F && buffer[2]==0x78)//If we get a "processing response" packet...
	{
		while(Serial.available()<3){}//wait to have the header or the minimum string length!!!
		byte len=Serial.read();
		chk=len;
		if (setspeed==1 || setspeed==0)
		{
			len=len-0x80;
			byte dest=Serial.read();
			byte orig=Serial.read();
			chk=chk+dest+orig;
		}
		for (byte cnt=0; cnt < len; cnt++)
		{
			while(Serial.available()==0){}//wait for the next byte
			buffer[cnt]=Serial.read();
		}
		while(Serial.available()==0){}//wait for the checksum
		byte chk2=Serial.read();
		chk=chk+iso_checksum(buffer,len);
		if (chk != chk2)
		{
		return 0;//checksum error
		}
	}
	  if (setspeed==0 || setspeed==1)
	  {
		delay(75);
	  }
	  else
	  {
		delay(4);
	  }
      return len;
    
  
}  

boolean KLINE::PIDwrite(byte data[],byte len)//write in slow speed mode (with delay between bytes)
{
  byte cnt=0;
  while (cnt < 3)
  {
    digitalWrite(READEN,HIGH);
    byte chks=0;
      byte a=0x68;
      chks=a+*address+*tester+iso_checksum(data,len);
      Serial.write(a);
      send_delay(3);
      Serial.write(*address);
      send_delay(3);
      Serial.write(*tester);
      send_delay(3);    
    for (byte cnt=0;cnt<len;cnt++)
    {
     Serial.write(data[cnt]);
	 send_delay(3);
    }
    Serial.write(chks);
	while (!(UCSR0A & _BV(TXC0)));//wait for data to be sent
    digitalWrite(READEN,LOW);
    int READ_ATTEMPTS=50;//try to read for approx 50 milisecond
    int t=0;
    while(t != READ_ATTEMPTS  &&  Serial.available() < 1)
    {
      delay(1);
      t++;
    }
    if (t < READ_ATTEMPTS)
    {
      return 1;
    }
    cnt++;//and will try to resend the packet 3 times
  }
  return 0;//if there is no reply, return 0
}


boolean KLINE::write(byte data[], byte len)//write in slow speed mode (with delay between bytes)
{
  byte cnt=0;
  while (cnt < 3)
  {
    digitalWrite(READEN,HIGH);
    byte chks=0;
    if (setspeed==1 || setspeed==0)//if we havent accessed a session...
    {
      byte a=0x80+len;
      chks=a+*address+*tester+iso_checksum(data,len);
      Serial.write(a);
      send_delay(13);
      Serial.write(*address);
      send_delay(13);
      Serial.write(*tester);
      send_delay(13);    
    }
    else//you might want to use this function in high speed mode, so try to get a diag session as soon as possible
    {
     chks=iso_checksum(data,len)+len;
     Serial.write(len);
    }
    for (byte cnt=0;cnt<len;cnt++)
    {
     Serial.write(data[cnt]);
	 if (setspeed<2)
	 {
		send_delay(13);
	 }
	 else
	 {
	   send_delay(0);
	 }
    }
    Serial.write(chks);
	while (!(UCSR0A & _BV(TXC0)));//wait for data to be sent
    digitalWrite(READEN,LOW);
    int READ_ATTEMPTS=1000;//try to read for approx 1 second
    int t=0;
    while(t != READ_ATTEMPTS  &&  Serial.available() < 1)
    {
      delay(1);
      t++;
    }
    if (t < READ_ATTEMPTS)
    {
      return 1;
    }
    cnt++;//and will try to resend the packet 3 times
  }
  return 0;//if there is no reply, return 0
}

boolean KLINE::write(byte data[], byte len1, byte data2[], byte len2)//write two arrays, useful for functions with header
{
  byte cnt=0;
  while (cnt < 3)
  {
    digitalWrite(READEN,HIGH);
    byte chks=0;
    if (setspeed==1 || setspeed==0)//if we havent accessed a session...
    {
      byte a=0x80+len1+len2;
      chks=a+*address+*tester+iso_checksum(data,len1)+iso_checksum(data2,len2);
      Serial.write(a);
      send_delay(13);
      Serial.write(*address);
      send_delay(13);
      Serial.write(*tester);
      send_delay(13);    
    }
    else//you might want to use this function in high speed mode, so try to get a diag session as soon as possible
    {
     chks=iso_checksum(data,len1)+iso_checksum(data2,len2)+(len1+len2);
     Serial.write(len1+len2);
    }
    for (byte cnt=0;cnt<len1;cnt++)
    {
     Serial.write(data[cnt]);
	 if (setspeed<2)
	 {
		send_delay(13);
	 }
	 else
	 {
	   send_delay(0);
	 }		
    }
	for (byte cnt=0;cnt<len2;cnt++)
    {
     Serial.write(data2[cnt]);
	 if (setspeed<2)
	 {
		send_delay(13);
	 }
	 else
	 {
	   send_delay(0);
	 }		
    }
    Serial.write(chks);
	while (!(UCSR0A & _BV(TXC0)));//wait for data to be sent
    digitalWrite(READEN,LOW);
    int READ_ATTEMPTS=1000;//try to read for approx 1 second
    int t=0;
    while(t != READ_ATTEMPTS  &&  Serial.available() < 1)
    {
      delay(1);
      t++;
    }
    if (t < READ_ATTEMPTS)
    {
      return 1;
    }
    cnt++;//and will try to resend the packet 3 times
  }
  return 0;//if there is no reply, return 0
}

boolean KLINE::sendDownloadData(byte data[], byte len)//Sends the data after download session is accepted
{
	/*Note that len cannot be more than 0xFE*/
	byte header[]={0x00,len+1,0x36};
	byte crc=iso_checksum(header,sizeof(header))+iso_checksum(data,len);
	digitalWrite(READEN,HIGH);
	for (byte cnt=0; cnt < sizeof(header);cnt++)
	{
		Serial.write(header[cnt]);
	}
	for (byte cnt=0; cnt < len; cnt++)
	{
		Serial.write(data[cnt]);
	}
	Serial.write(crc);
	byte reply[4];
	while (!(UCSR0A & _BV(TXC0)));//wait for data to be sent
	digitalWrite(READEN,LOW);
	read(reply);
	if (reply[0]==0x76 || reply[2]==0x76 )
	{
		return 1;
	}
	return 0;
}
	


byte KLINE::getIDData(byte data[])//Gets the data after upload session is accepted
{
	/*Note that len wont be more than 0xFE*/
	//len=len+2;
	while (Serial.available()<4){}//wait for the min header
	byte len;
	byte t=Serial.read();
	byte chk;
	if (t==0x00)//support for huge memory readouts
	{
		len=Serial.read();
		t=Serial.read();//clean the extra bytes
		if (t!=0x61)
		{
		  return 0;
		}
		chk=len+t;
		len=len-2;
		t=Serial.read();
		chk=chk+t;
	}
	else
	{//support for standard response
		chk=t;
		len=t-2;
		t=Serial.read();//clean the extra byte
		chk=chk+t;
		if (t!=0x61)
		{
		  return 0;
		}
		t=Serial.read();
		chk=chk+t;
	}
	for (byte cnt=0;cnt < len; cnt++)
	{
		while (Serial.available()==0){}//wait for the byte
		data[cnt]=Serial.read();
	}
	while (Serial.available()==0){}//wait for checksum
	byte chk2=Serial.read();
	chk=chk+iso_checksum(data,len);
	if (chk != chk2)
	{
		return 0;
	}
	else
	return len;
}		

byte KLINE::getUploadData(byte data[])//Gets the data after upload session is accepted
{
	/*Note that len wont be more than 0xFE*/
	//len=len+2;
	while (Serial.available()<4){}//wait for the min header
	byte len;
	byte t=Serial.read();
	byte chk;
	if (t==0x00)//support for huge memory readouts
	{
		len=Serial.read();
		t=Serial.read();//clean the extra bytes
		if (t!=0x36)
		{
		  return 0;
		}
		chk=len+t;
		len=len-1;
	}
	else
	{//support for standard response
		chk=t;
		len=t-1;
		t=Serial.read();//clean the extra byte
		chk=chk+t;
		if (t!=0x36)
		{
		  return 0;
		}
	}
	for (byte cnt=0;cnt < len; cnt++)
	{
		while (Serial.available()==0){}//wait for the byte
		data[cnt]=Serial.read();
	}
	while (Serial.available()==0){}//wait for checksum
	byte chk2=Serial.read();
	chk=chk+iso_checksum(data,len);
	if (chk != chk2)
	{
		return 0;
	}
	else
	return len;
}	
	


byte KLINE::iso_checksum(byte *data, long len)
{
  byte crc=0;
  for(int i=0; i<len; i++)
  {
    crc=crc+data[i];
  }
  return crc;
}