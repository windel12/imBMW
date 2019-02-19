using System;
using imBMW.iBus;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Linq;

namespace System.IO.Ports
{
    public class SerialPortTH3122 : SerialInterruptPort
    {
        private byte[] data;
        private int readAvailable;
        private object _lock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="busy"></param>
        /// <param name="fixParity">Set true if the data is corrupted because of parity bit issue in Cerberus software.</param>
        public SerialPortTH3122(String port, Cpu.Pin busy, bool fixParity = false, int baudRate = (int)BaudRate.Baudrate9600, int writeBufferSize = 0, ushort readBufferSize = Message.PacketLengthMax) :
            base(new SerialPortConfiguration(port, baudRate, Parity.Even, 8 + (fixParity ? 1 : 0), StopBits.One), busy, writeBufferSize, readBufferSize, 50)
        {
            AfterWriteDelay = 4;
            data = new byte[0];
        }

        public override void Flush()
        {
            if(_port.IsOpen)
                _port.DiscardOutBuffer();
        }

        protected override int WriteDirect(byte[] data, int offset, int length)
        {
            if (!_port.IsOpen) 
            {
                //this.data = data;
                lock (_lock)
                {
                    if (offset == 0)
                    {
                        readAvailable = 0;
                        this.data = new byte[data.Length];
                    }

                    Array.Copy(data, offset, this.data, readAvailable, length);
                    readAvailable += length;
                    return readAvailable;
                }
            }
            return base.WriteDirect(data, offset, length);
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            if (!_port.IsOpen)
            {
                lock (_lock)
                {
                    Array.Copy(this.data, data, this.data.Length);
                    return this.data.Where(x => x != 0x00).Count();
                }
            }
            return _port.Read(data, 0, _port.BytesToRead);
        }

        public override byte[] ReadAvailable(int maxCount)
        {
            if (!_port.IsOpen)
            {
                lock (_lock)
                {
                    var temp = new byte[this.readAvailable];
                    Array.Copy(this.data, temp, this.readAvailable);

                    Array.Clear(this.data, 0, readAvailable);
                    readAvailable = 0;
                    return temp;
                }
            }
            return base.ReadAvailable(maxCount);
        }

        public override string ToString()
        {
            return _port.PortName;
        }
    }
}
