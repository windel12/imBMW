using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace System.IO.Ports
{
    public class SerialPortTH3122 : SerialInterruptPort
    {
        private byte[] data;
        private object _lock = new object();

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
            data = new byte[0];
        }

        public override void Flush()
        {
            _port.DiscardOutBuffer();
        }

        protected override int WriteDirect(byte[] data, int offset, int length)
        {
            if (!_port.IsOpen) 
            {
                //this.data = data;
                lock (_lock)
                {
                    this.data = new byte[length];
                    Array.Copy(data, offset, this.data, 0, length);
                    return this.data.Length;
                }
            }
            return base.WriteDirect(data, offset, length);
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            if (!_port.IsOpen)
            {
                return this.data.Length;
            }
            return _port.Read(data, 0, _port.BytesToRead);
        }

        public override byte[] ReadAvailable(int maxCount)
        {
            if (!_port.IsOpen)
            {
                lock (_lock)
                {
                    var temp = new byte[this.data.Length];
                    Array.Copy(this.data, temp, this.data.Length);
                    this.data = new byte[0];
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
