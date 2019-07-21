using System;
using System.IO.Ports;
using System.Threading;
using GHI.Usb.Host;
using imBMW.Tools;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHI.Pins;
using imBMW.Diagnostics;

namespace imBMW.DBus.Tester
{
    public class Program2
    {
        public static UsbSerial usbSerialDevice = null;

        private static OutputPort led2 = new OutputPort(FEZPandaIII.Gpio.Led2, false);
        private static OutputPort led3 = new OutputPort(FEZPandaIII.Gpio.Led3, false);

        public static void Main2()
        {
            Controller.UsbSerialConnected += Controller_UsbSerialConnected;
            Controller.UnknownDeviceConnected += Controller_UnknownDeviceConnected;
            Controller.DeviceConnectFailed += Controller_DeviceConnectFailed;

            Controller.Start();
            Debug.Print("Controller started");

            while (true)
            {
                if (usbSerialDevice != null)
                {
                    DBusMessage test = new DBusMessage(iBus.DeviceAddress.OBD, iBus.DeviceAddress.DDE, 0x2C, 0x10, 0x0F, 0x00);
                    usbSerialDevice.Write(test.Packet);
                }
                Thread.Sleep(1000);
            }
        }

        private static void Controller_UsbSerialConnected(object sender, UsbSerial usbSerial)
        {
            Debug.Print("Detected a USB to serial adaptor of type:" + usbSerial.Type.ToString());

            led2.Write(true);

            // The newly connected device is a USB to serial adapter:
            usbSerialDevice = usbSerial;
            usbSerialDevice.BaudRate = 9600;
            usbSerialDevice.DataBits = 8;

            usbSerial.Handshake = Handshake.None;
            usbSerialDevice.Parity = Parity.None;
            usbSerialDevice.StopBits = StopBits.One;

            usbSerialDevice.Disconnected += Controller_Disconnected;
            usbSerialDevice.DataReceived += UsbSerialDevice_DataReceived;
        }

        static void Controller_UnknownDeviceConnected(object sender, Controller.UnknownDeviceConnectedEventArgs e)
        {
            Debug.Print("USB raw device connected");
            led3.Write(true);
        }

        static void Controller_DeviceConnectFailed(object sender, EventArgs e)
        {
            Debug.Print("USB device connect failed");
            led3.Write(true);
        }

        static void Controller_Disconnected(BaseDevice sender, EventArgs e)
        {
            Debug.Print("USB device disconnected");
            usbSerialDevice = null;
        }

        static void UsbSerialDevice_DataReceived(UsbSerial sender, UsbSerial.DataReceivedEventArgs e)
        {
            Debug.Print("USB data received" + e.Data.ToHex());

            //for (int i = 0; i < e.Data.Length; i++)
            //    Debug.Print(e.Data[i].ToString());

            //sender.Write(e.Data);
        }
    }
}
