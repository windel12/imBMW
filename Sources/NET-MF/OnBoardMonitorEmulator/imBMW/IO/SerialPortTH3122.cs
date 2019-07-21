using System;
using imBMW.iBus;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Linq;

namespace System.IO.Ports
{
    public class SerialPortTH3122 : SerialInterruptPort
    {
        private byte[] writeBuffer = new byte[0];
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
        }

        public override void Flush()
        {
            if (_port.IsOpen)
                _port.DiscardOutBuffer();
        }

        public override void Write(byte[] newData, int offset, int length)
        {
            if (!_port.IsOpen)
            {
                lock (_lock)
                {
                    base.Write(newData, offset, length);
                }
            }
            else
            {
                base.Write(newData, offset, length);
            }
        }

        protected override int WriteDirect(byte[] newData, int offset, int length)
        {
            if (!_port.IsOpen)
            {
                    var temp = new byte[writeBuffer.Length];
                    Array.Copy(writeBuffer, temp, temp.Length);

                    writeBuffer = new byte[writeBuffer.Length + length];
                    Array.Copy(temp, writeBuffer, temp.Length);
                    Array.Copy(newData, offset, writeBuffer, temp.Length, length);

                    return length;
            }

            return base.WriteDirect(newData, offset, length);
        }

        protected override int ReadDirect(byte[] buffer, int offset, int length)
        {
            if (!_port.IsOpen)
            {
                lock (_lock)
                {
                    Array.Clear(buffer, 0, buffer.Length);

                    Array.Copy(this.writeBuffer, buffer, this.writeBuffer.Length);
                    int readAmount = this.writeBuffer.Length;
                    this.writeBuffer = new byte[0];
                    return readAmount;//.Where(x => x != 0x00).Count();
                }
            }

            return _port.Read(buffer, 0, _port.BytesToRead);
        }

        public override string ToString()
        {
            return _port.PortName;
        }
    }
}
