using System;
using System.IO.Ports;
using System.Threading;
using GHI.Pins;
using imBMW.Diagnostics;
using imBMW.iBus;
using imBMW.Tools;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace imBMW.DBus.Tester
{
    public class Program
    {
        static OutputPort LED;
        static OutputPort led4;

        static byte busy = 0;

        public static void Main()
        {
            LED = new OutputPort((Cpu.Pin)78, false);
            led4 = new OutputPort((Cpu.Pin)73, false);

            //KWP2000Init();
            DBusMessage motor_temperatur = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10, 0x0F, 0x00);
            //serial.Write(motor_temperatur.Packet, 0, motor_temperatur.Packet.Length);

            ISerialPort dBusPort = new SerialInterruptPort(new SerialPortConfiguration("COM4", 9600, Parity.Even, 8, StopBits.One), Cpu.Pin.GPIO_NONE, 1, 258); // d31, d33
            dBusPort.AfterWriteDelay = 4;
            DBusManager.Init(dBusPort);

            DBusManager.Instance.BeforeMessageReceived += Manager_BeforeMessageReceived;
            DBusManager.Instance.AfterMessageReceived += Manager_AfterMessageReceived;
            DBusManager.Instance.BeforeMessageSent += Manager_BeforeMessageSent;
            DBusManager.Instance.AfterMessageSent += Manager_AfterMessageSent;

            bool b = true;
            while (true)
            {
                if(b)
                    DBusManager.Instance.EnqueueMessage(motor_temperatur);
                Thread.Sleep(500);
            }
        }

        private static void KWP2000Init()
        {
            byte address = 0x33;
            var tx = new OutputPort(FEZPandaIII.Gpio.D33, false);
            tx.Write(true);
            Thread.Sleep(300);
            tx.Write(false);
            Thread.Sleep(200);
            for (byte mask = 0x01; mask > 0; mask <<= 1) // Send byte 0x33 to the K-Line at 5 baud,
            {
                if ((address & mask) == 1)
                {
                    tx.Write(true);
                }
                else
                {
                    tx.Write(false);
                }
                Thread.Sleep(200);
            }
            tx.Write(true);

            var serial = new SerialPort("COM4", 10400, Parity.None, 8, StopBits.One); // Initialize UART to 10.4K baud, 8 data bits, no parity, and 1 stop.
            serial.ReadTimeout = 0;

            int delayy = 0;
            while (serial.BytesToRead < 1 && delayy < 2000)
            {
                Thread.Sleep(1);
                delayy++;
            }

            int b = serial.ReadByte();
            if (b == 0x55) { } // Receive byte 0x55 from the vehicle.
            //else
            //{
            //    Serial.begin(10400);//correct baudrate!
            //}
            while (serial.BytesToRead < 2) { }
            for (int j = 0; j < 2; j++) // Receive two key bytes, which are either 08 08 or 94 94 for ISO 9141-2.
            {
                b = serial.ReadByte();
            }

            Thread.Sleep(40); // Wait for about 40 milliseonds.
            serial.WriteByte((byte)~b); // Then, Invert key byte 2 and send to the vehicle.

            Thread.Sleep(40); //Wait another 40 milliseconds.
            b = serial.ReadByte(); // Then receive inverted address byte 0x33, which will be 0xCC.
            int u = ~address;
            if (b == u)
            {

            }

            Thread.Sleep(65); // Before sending the first request, wait for about 65 milliseconds.
        }

        private static void Manager_BeforeMessageReceived(MessageEventArgs e)
        {
            LED.Write(Busy(true, 1));
        }

        private static void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            LED.Write(Busy(false, 1));

            if (e.Message.Data.Compare(MessageRegistry.DataAnnounce)
                || e.Message.Data.Compare(MessageRegistry.DataPollRequest)
                || e.Message.Data.Compare(MessageRegistry.DataPollResponse))
            {
                return;
            }

            // Show only messages which are described
            if (e.Message.Describe() == null) { return; }

            if ((
                    e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.CDChanger ||
                    e.Message.SourceDevice == DeviceAddress.CDChanger && e.Message.DestinationDevice == DeviceAddress.Radio ||
                    e.Message.SourceDevice == DeviceAddress.GraphicsNavigationDriver || e.Message.DestinationDevice == DeviceAddress.GraphicsNavigationDriver ||
                    e.Message.SourceDevice == DeviceAddress.OnBoardMonitor || e.Message.DestinationDevice == DeviceAddress.OnBoardMonitor ||
                    e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics || e.Message.DestinationDevice == DeviceAddress.InstrumentClusterElectronics ||
                    e.Message.SourceDevice == DeviceAddress.NavigationEurope || e.Message.DestinationDevice == DeviceAddress.NavigationEurope ||
                    e.Message.SourceDevice == DeviceAddress.NavigationEurope || e.Message.DestinationDevice == DeviceAddress.NavigationEurope
                )
                //&& e.Message.SourceDevice != DeviceAddress.GraphicsNavigationDriver
                //&& e.Message.DestinationDevice != DeviceAddress.GraphicsNavigationDriver
                //&& e.Message.SourceDevice != DeviceAddress.OnBoardMonitor
                //&& e.Message.DestinationDevice != DeviceAddress.OnBoardMonitor
                //&& e.Message.SourceDevice != DeviceAddress.CDChanger
                //&& e.Message.DestinationDevice != DeviceAddress.CDChanger
                && e.Message.DestinationDevice != DeviceAddress.LocalBroadcastAddress
                //&& e.Message.SourceDevice != DeviceAddress.Diagnostic
                //&& e.Message.DestinationDevice != DeviceAddress.Diagnostic
            )
            {
                var logIco = "< ";
                Logger.Trace(e.Message.ToPrettyString(true, true), logIco);
                //Logger.Info(e.Message, logIco);
            }
        }

        private static void Manager_BeforeMessageSent(MessageEventArgs e)
        {
            LED.Write(Busy(true, 2));
        }

        private static void Manager_AfterMessageSent(MessageEventArgs e)
        {
            LED.Write(Busy(false, 2));
            Logger.Trace(e.Message, " >");
        }

        static bool Busy(bool busy, byte type)
        {
            if (busy)
            {
                Program.busy = Program.busy.AddBit(type);
            }
            else
            {
                Program.busy = Program.busy.RemoveBit(type);
            }
            return Program.busy > 0;
        }
    }
}
