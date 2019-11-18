using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using System.Threading;
using imBMW.Diagnostics;

namespace AuxilaryHeaterEmulator
{
    class Program
    {
        static object bufferSync = new object();
        private static int timeout = -1;

        static void Main(string[] args)
        {
            SerialPort port = new SerialPort("COM2");
            //port.ReadTimeout = 0;
            port.WriteTimeout = 1;
            port.BaudRate = 9600;
            port.DataBits = 8;
            port.Parity = Parity.Even;
            port.StopBits = StopBits.One;
            port.Open();

            port.DataReceived += Port_DataReceived;
            port.ErrorReceived += Port_ErrorReceived;


            while (true)
            {
                string value = Console.ReadLine();
                if(int.TryParse(value, out timeout))
                {
                    
                }
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
            if (receivedMessage == null)
            {
                DS2Message ds2Message = DS2Message.TryCreate(data);
                if (ds2Message == null)
                {
                    var value = string.Concat(data.Select(x => x.ToString("X2") + " "));
                    Console.WriteLine(value);
                    return;
                }
                receivedMessage = ds2Message.ToIKBusMessage();
            }
            
            Console.WriteLine(receivedMessage.ToString());
            if(receivedMessage.Data.StartsWith(AuxilaryHeater.DataZuheizerStatusRequest))
            {
                Message message = new Message(receivedMessage.DestinationDevice, receivedMessage.SourceDevice,
                    0xA0, 0xC8, 0x38, 0x33, 0x68, 0x01, 0x01, 0x01, 0x0A, 0x47, 0x97, 0xD9, 0x13, 0x00
                );
                if (timeout == -1)
                {
                    port.Write(message.Packet, 0, message.Packet.Length);
                }
                else
                {
                    foreach (var _byte in message.Packet)
                    {
                        port.Write(new byte[1] {_byte}, 0, 1);
                        Thread.Sleep(timeout);
                    }
                }
                Console.WriteLine(message.ToString());
            }
        }

        private static void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine("Error!!!");
        }
    }
}
