using System;
using Microsoft.SPOT.IO;

namespace GHI.Usb.Host
{
    public static class Controller
    {
        //
        // Summary:
        //     Raised when a device fails to connect properly.
        public static event DeviceConnectFailedEventHandler DeviceConnectFailed;
        //
        // Summary:
        //     Raised when a joystick connects.
        public static event JoystickConnectedEventHandler JoystickConnected;
        //
        // Summary:
        //     Raised when a keyboard connects.
        public static event KeyboardConnectedEventHandler KeyboardConnected;
        //
        // Summary:
        //     Raised when a mass storage device connects.
        public static event MassStorageConnectedEventHandler MassStorageConnected;
        //
        // Summary:
        //     Raised when a mouse connects.
        public static event MouseConnectedEventHandler MouseConnected;
        //
        // Summary:
        //     Raised when a raw device connects.
        public static event UnknownDeviceConnectedEventHandler UnknownDeviceConnected;
        //
        // Summary:
        //     Raised when a usb serial converter connects.
        public static event UsbSerialConnectedEventHandler UsbSerialConnected;
        //
        // Summary:
        //     Raised when a webcam connects.
        public static event WebcamConnectedEventHandler WebcamConnected;

        //
        // Summary:
        //     Gets a list of the currently connected devices.
        //
        // Returns:
        //     The currently connected devices.
        //public static BaseDevice[] GetConnectedDevices();
        //
        // Summary:
        //     Gets the last USB error that occured.
        //
        // Returns:
        //     The error that occured.
        public static Error GetLastError()
        {
            return Error.CompletionCodeBitStuffing;
        }

        //
        // Summary:
        //     Resets the USB host controller.
        public static void Reset() { }
        //
        // Summary:
        //     Starts the USB Host controller.
        public static void Start()
        {
            var e = MassStorageConnected;
            if (e != null)
            {
                e(null, new MassStorage());
            }
            //var ee = UnknownDeviceConnected;
            //if (ee != null)
            //{
            //    ee(null, new UnknownDeviceConnectedEventArgs());
            //}
        }

        //
        // Summary:
        //     USB host errors.
        public enum Error : uint
        {
            //
            // Summary:
            //     No error.
            NoError = 0,
            //
            // Summary:
            //     Device is busy. Try communicating with the device at a later time.
            DeviceBusy = 1,
            //
            // Summary:
            //     Transfer Error. Try Transferring again.
            TransferError = 2,
            //
            // Summary:
            //     Maximum available handles reached.
            MaxDeviceUsage = 3,
            //
            // Summary:
            //     Device is not connected.
            DeviceNotOnline = 4,
            //
            // Summary:
            //     Out of memory.
            OutOfMemory = 5,
            //
            // Summary:
            //     Maximum USB devices connected (127).
            MaxUsbDevicesReached = 6,
            //
            // Summary:
            //     HID parse error.
            HIDParserError = 7,
            //
            // Summary:
            //     HID item not found.
            HIDParserItemNotFound = 8,
            //
            // Summary:
            //     Transfer completed successfully.
            CompletionCodeNoError = 268435456,
            //
            // Summary:
            //     Transfer error. Make sure you have enough power for the USB device and connections
            //     are stable.
            CompletionCodeCRC = 268435457,
            //
            // Summary:
            //     Transfer error. Make sure you have enough power for the USB device and connections
            //     are stable.
            CompletionCodeBitStuffing = 268435458,
            //
            // Summary:
            //     Transfer error. Make sure you have enough power for the USB device and connections
            //     are stable. This error means there might be some missing USB packets during communications.
            //     In many cases you can ignore this error if missing some packets is not significant.
            //     Several USB devices might drop some packets or incorrectly produce this error.
            CompletionCodeDataToggle = 268435459,
            //
            // Summary:
            //     Transfer error. USB device refused the transfer. Check sent USB packet.
            CompletionCodeStall = 268435460,
            //
            // Summary:
            //     Transfer error. Make sure you have enough power for the USB device and connections
            //     are stable.
            CompletionCodeNoResponse = 268435461,
            //
            // Summary:
            //     Transfer error. Make sure you have enough power for the USB device and connections
            //     are stable.
            CompletionCodePIDCheck = 268435462,
            //
            // Summary:
            //     Transfer error. Make sure you have enough power for the USB device and connections
            //     are stable.
            CompletionCodePIDUnExpected = 268435463,
            //
            // Summary:
            //     Transfer error. Endpoint returned more data than expected.
            CompletionCodeDataOverRun = 268435464,
            //
            // Summary:
            //     Transfer error. Endpoint returned less data than expected.
            CompletionCodeDataUnderRun = 268435465,
            //
            // Summary:
            //     Transfer error. HC received data from endpoint faster than it could be written
            //     to system memory.
            CompletionCodeBufferOverRun = 268435466,
            //
            // Summary:
            //     Transfer error. HC could not retrieve data from system memory fast enough to
            //     keep up with data USB data rate.
            CompletionCodeBufferUnderRun = 268435467,
            //
            // Summary:
            //     Software use.
            CompletionCodeNotAccessed = 268435468,
            //
            // Summary:
            //     Software use.
            CompletionCodeNotAccessedF = 268435469,
            //
            // Summary:
            //     Mass Storage error.
            MSError = 536870912,
            //
            // Summary:
            //     Mass Storage error.
            MSCSWCommandFailed = 536870913,
            //
            // Summary:
            //     Mass Storage error.
            MSCSWStatusPhaseError = 536870914,
            //
            // Summary:
            //     Mass Storage error.
            MSCSW = 536870915,
            //
            // Summary:
            //     Mass Storage error.
            MSWrongLunNumber = 536870916,
            //
            // Summary:
            //     Mass Storage error.
            MSWrongSignature = 536870917,
            //
            // Summary:
            //     Mass Storage error.
            MSTagMissmatched = 536870918,
            //
            // Summary:
            //     Mass Storage error.
            MSNotReady = 536870919
        }

        public class UnknownDeviceConnectedEventArgs : EventArgs
        {
            //
            // Summary:
            //     The device id.
            public uint Id { get; }
            //
            // Summary:
            //     The logical device interface index.
            public byte InterfaceIndex { get; }
            //
            // Summary:
            //     The device's USB port number.
            public byte PortNumber { get; }
            //
            // Summary:
            //     The device's product id.
            public ushort ProductId { get; }
            //
            // Summary:
            //     The device's type.
            //public BaseDevice.DeviceType Type { get; }
            //
            // Summary:
            //     The devic's vendor id.
            public ushort VendorId { get; }
        }

        public delegate void DeviceConnectFailedEventHandler(object sender, EventArgs e);
        public delegate void JoystickConnectedEventHandler(object sender, EventArgs e);
        public delegate void KeyboardConnectedEventHandler(object sender, EventArgs e);
        public delegate void MassStorageConnectedEventHandler(object sender, MassStorage e);
        public delegate void MouseConnectedEventHandler(object sender, EventArgs e);
        public delegate void UnknownDeviceConnectedEventHandler(object sender, UnknownDeviceConnectedEventArgs e);
        public delegate void UsbSerialConnectedEventHandler(object sender, EventArgs e);
        public delegate void WebcamConnectedEventHandler(object sender, EventArgs e);
    }
}
