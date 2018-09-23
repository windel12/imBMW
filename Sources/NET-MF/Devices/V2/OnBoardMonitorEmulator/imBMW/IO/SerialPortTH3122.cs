
using Microsoft.SPOT.Hardware;
using System.IO.Ports;

namespace System.IO.Ports
{
    public class SerialPortTH3122 : ISerialPort
    {
        public SerialPortTH3122(String port, Cpu.Pin busy)
        {   
        }

        private byte[] data;

        public void Write(params byte[] data)
        {
            this.data = data;

            if (DataReceived != null)
            {
                DataReceived(this, new SerialDataReceivedEventArgs());
            }
        }

        public void Write(byte[] data, int offset, int length)
        {
            Write(data);
        }

        public void Write(string text)
        {
            
        }

        public void WriteLine(string text)
        {
            
        }

        public int AvailableBytes => data.Length;

        public int AfterWriteDelay { get; set; }

        public int ReadTimeout { get; set; }

        public byte[] ReadAvailable() => data;

        public byte[] ReadAvailable(int maxCount)
        {
            return ReadAvailable();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return data.Length;
        }

        public string ReadLine()
        {
            return "";
        }

        public void Flush()
        {
            
        }

        public event SerialDataReceivedEventHandler DataReceived;

        public event BusyChangedEventHandler BusyChanged;
    }
}
