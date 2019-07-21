using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace AuxilaryHeaterEmulator
{
    class Program
    {
        static object bufferSync = new object();

        static void Main(string[] args)
        {
            SerialPort port = new SerialPort("COM5");
            //port.ReadTimeout = 0;
            port.WriteTimeout = 1;
            port.BaudRate = 9600;
            port.DataBits = 8;
            port.Parity = Parity.Even;
            port.StopBits = StopBits.One;
            port.Open();

            port.DataReceived += Port_DataReceived;


            while (true)
            {
                //var data = Console.ReadLine();
                //port.Write(data);
            }
        }

        private static void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;
            if (port.BytesToRead == 0)
            {
                return;
            }
            byte[] data = new byte[port.BytesToRead];
            lock (bufferSync)
            {
                port.Read(data, 0, data.Length);
            }
            Message receivedMessage = Message.TryCreate(data);
            if(receivedMessage.Data.StartsWith(AuxilaryHeater.DataZuheizerStatusRequest))
            {
                Message message = new Message(receivedMessage.DestinationDevice, receivedMessage.SourceDevice,
                    0xA0, 0xC8, 0x38, 0x33, 0x68, 0x01, 0x01, 0x01, 0x0A, 0x47, 0x97, 0xD9, 0x13, 0x00
                );
                port.Write(message.Packet, 0, message.Packet.Length);
            }
        }
    }
}
