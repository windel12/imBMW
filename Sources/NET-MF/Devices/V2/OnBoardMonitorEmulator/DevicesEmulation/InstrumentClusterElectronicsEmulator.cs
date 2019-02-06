using System;
using System.Threading;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class InstrumentClusterElectronicsEmulator
    {
        private static Timer rpmSpeedAnounceTimer;
        private static Timer temperatureAnounceTimer;

        private static byte rpmSpeedAnounceTimerInterval = 1;//2;
        private static byte temperatureAnounceTimerIterval = 1;//10;

        private static byte CurrentSpeed = 0;
        private static byte CurrentRPM = 0;
        private static byte TemperatureOutside = 15;
        private static byte TemperatureCoolant = 5;

        public static void Init() { }

        static InstrumentClusterElectronicsEmulator()
        {
            Manager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, ProcessMessageFromGraphicNavigationDriver);

            DBusManager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.OBD, ProcessDS2MessageAndForwardToIBus);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Diagnostic, ProcessDiagnosticMessageFromIBusAndForwardToDBus);
        }

        private static void GenerateRpmSpeedTemp()
        {
            var random = new Random();
            CurrentSpeed = (byte)random.Next(0x00, 0xFF);
            CurrentRPM = (byte)random.Next(0x00, 44);
            TemperatureOutside++;
            TemperatureCoolant++;
        }

        public static void StartAnounce()
        {
            rpmSpeedAnounceTimer = new Timer((state) =>
            {
                GenerateRpmSpeedTemp();
                var rpmSpeedMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, 0x18, CurrentSpeed, CurrentRPM);
                Manager.EnqueueMessage(rpmSpeedMessage);
                KBusManager.Instance.EnqueueMessage(rpmSpeedMessage);
            }, null, 0, rpmSpeedAnounceTimerInterval * 1000);

            temperatureAnounceTimer = new Timer((state) =>
            {
                GenerateRpmSpeedTemp();
                var temperatureMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, 0x19, TemperatureOutside, TemperatureCoolant, 0x00);
                Manager.EnqueueMessage(temperatureMessage);
                KBusManager.Instance.EnqueueMessage(temperatureMessage);

            }, null, 0, temperatureAnounceTimerIterval * 1000);
        }

        static void ProcessMessageFromGraphicNavigationDriver(Message m)
        {
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestTime.Data))
            {
                // 16:58
                Manager.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x24, 0x01, 0x00, 0x31, 0x36, 0x3A, 0x34, 0x38, 0x20, 0x20));
            }
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestDate.Data))
            {
                // 06/26/2016"
                Manager.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x24, 0x02, 0x00, 0x30, 0x36, 0x2F, 0x32, 0x36, 0x2F, 0x32, 0x30, 0x31, 0x36));
            }
        }

        static void ProcessDS2MessageAndForwardToIBus(Message m)
        {
            // do not forward responses from modules
            if (m.Data[0] == 0xA0 || m.DestinationDevice == DeviceAddress.DDE || m.DestinationDevice == DeviceAddress.ElectronicGearbox || m.DestinationDevice == DeviceAddress.ASC)
            {
                return;
            }

            Thread.Sleep(100);
            var message = new Message(DeviceAddress.Diagnostic, m.DestinationDevice, m.Data);
            Manager.EnqueueMessage(message);
        }

        static void ProcessDiagnosticMessageFromIBusAndForwardToDBus(Message m)
        {
            if (m.Data[0] == 0xA0 && m.SourceDevice != DeviceAddress.NavigationEurope) // 0xA0 - DIAG OKAY
            {
                Thread.Sleep(100);
                var message = new DS2Message(m.SourceDevice, m.Data);
                DBusManager.Instance.EnqueueMessage(message);
            }
        }
    }
}
