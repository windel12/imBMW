using System;
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
        public SerialPortTH3122(String port, Cpu.Pin busy, bool fixParity = false, int baudRate = (int)BaudRate.Baudrate9600, int writeBufferSize = 0) :
            base(new SerialPortConfiguration(port, baudRate, Parity.Even, 8 + (fixParity ? 1 : 0), StopBits.One), busy, writeBufferSize, imBMW.iBus.Message.PacketLengthMax, 50)
        {
            AfterWriteDelay = 4;
        }
    }
}
