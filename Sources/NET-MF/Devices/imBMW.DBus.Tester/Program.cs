using System;
using System.IO.Ports;
using imBMW.Devices.V2.Hardware;
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
            LED = new OutputPort(Pin.LED, false);
            led4 = new OutputPort(Pin.LED4, false);

            ISerialPort dBusPort = new SerialPortTH3122("COM4", Pin.D_BUS_TH3122SENSTA);
            DbusManager.Init(dBusPort);

            DbusManager.BeforeMessageReceived += Manager_BeforeMessageReceived;
            DbusManager.AfterMessageReceived += Manager_AfterMessageReceived;
            DbusManager.BeforeMessageSent += Manager_BeforeMessageSent;
            DbusManager.AfterMessageSent += Manager_AfterMessageSent;
        }

        private static void Manager_BeforeMessageReceived(MessageEventArgs e)
        {
            LED.Write(Busy(true, 1));
        }

        static bool restrictOutput = true;
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
            if (!restrictOutput)
            {
                var logIco = "< ";
                Logger.Info(e.Message.ToPrettyString(true, true), logIco);
                //Logger.Info(e.Message, logIco);
                return;
            }

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
                && e.Message.DestinationDevice != DeviceAddress.Broadcast
                //&& e.Message.SourceDevice != DeviceAddress.Diagnostic
                //&& e.Message.DestinationDevice != DeviceAddress.Diagnostic
            )
            {
                var logIco = "< ";
                Logger.Info(e.Message.ToPrettyString(true, true), logIco);
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
            Logger.Info(e.Message, " >");
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
