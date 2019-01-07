using System;
using imBMW.iBus;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace System.IO.Ports
{
    public class SerialPortTH3122 : SerialInterruptPort
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="busy"></param>
        /// <param name="fixParity">Set true if the data is corrupted because of parity bit issue in Cerberus software.</param>
        public SerialPortTH3122(String port, Cpu.Pin busy, bool fixParity = false, ushort baudRate = (ushort)BaudRate.Baudrate9600, ushort writeBufferSize = 0) :
            base(new SerialPortConfiguration(port, baudRate, Parity.Even, 8 + (fixParity ? 1 : 0), StopBits.One), busy, writeBufferSize, Message.PacketLengthMax, 50)
        {
            AfterWriteDelay = 4;
        }
    }
}
