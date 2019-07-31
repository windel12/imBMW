using System;
using System.Text;
using System.Threading;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class InstrumentClusterElectronicsEmulator
    {
        public static Timer rpmSpeedAnounceTimer;
        public static Timer temperatureAnounceTimer;

        private static byte rpmSpeedAnounceTimerInterval = 2;//2;
        private static byte temperatureAnounceTimerIterval = 10;//10;

        private static byte CurrentSpeed = 0;
        private static ushort CurrentRPM = 0;
        private static byte TemperatureOutside = 15;
        private static byte TemperatureCoolant = 5;

        public delegate void ShowOBCMessageEventHandler(string message);
        public static event ShowOBCMessageEventHandler OBCTextChanged;

        private static IgnitionState _ignitionState;
        public static IgnitionState IgnitionState
        {
            get { return _ignitionState; }
            set
            {
                _ignitionState = value;
                var ignitionChangedMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, 0x11, GetIgnitionStateByte(_ignitionState));
                Manager.Instance.EnqueueMessage(ignitionChangedMessage);
            }
        }

        public static void Init() { }

        static InstrumentClusterElectronicsEmulator()
        {
            Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.InstrumentClusterElectronics, ProcessMessageToIKE);

            DBusManager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.OBD, ProcessDS2MessageAndForwardToIBus);
            Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.Diagnostic, ProcessDiagnosticMessageFromIBusAndForwardToDBus);

            TemperatureOutside = (byte)(new Random().Next(0, 40));
        }

        public static void StartAnounce()
        {
            rpmSpeedAnounceTimer = new Timer((state) =>
            {
                var random = new Random();
                CurrentSpeed++;// (byte)random.Next(0, 160);
                CurrentRPM = (ushort)random.Next(800, 4400);

                var rpmSpeedMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, 0x18, (byte)(CurrentSpeed / 2), (byte)(CurrentRPM / 100));
                Manager.Instance.EnqueueMessage(rpmSpeedMessage);
                if (KBusManager.Instance.Inited)
                    KBusManager.Instance.EnqueueMessage(rpmSpeedMessage);
            }, null, 0, rpmSpeedAnounceTimerInterval * 1000);

            temperatureAnounceTimer = new Timer((state) =>
            {
                TemperatureCoolant += 5;
                
                var temperatureMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, 0x19, TemperatureOutside, TemperatureCoolant, 0x00);
                Manager.Instance.EnqueueMessage(temperatureMessage);
                if(KBusManager.Instance.Inited)
                    KBusManager.Instance.EnqueueMessage(temperatureMessage);

            }, null, 0, temperatureAnounceTimerIterval * 1000);
        }

        public static void StopAnnounce()
        {
            rpmSpeedAnounceTimer.Dispose();
            temperatureAnounceTimer.Dispose();
        }

        public static void ProcessMessageToIKE(Message m)
        {
            if (m.SourceDevice == DeviceAddress.Radio && m.Data.StartsWith(0x23, 0x62, 0x30))
            {
                byte[] messageData = m.Data.Skip(3);
                string message = System.Text.ASCIIEncoding.GetString(messageData);
                var e = OBCTextChanged;
                if (e != null)
                {
                    e(message);
                }
            }
            if (m.SourceDevice == DeviceAddress.Telephone && m.Data.StartsWith(0x23, 0x42, 0x30))
            {
                byte[] messageData = m.Data.Skip(3);
                string message = System.Text.ASCIIEncoding.GetString(messageData);
                var e = OBCTextChanged;
                if (e != null)
                {
                    e(message);
                }
            }
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestIgnitionStatus.Data))
            {
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, 0x11, GetIgnitionStateByte(IgnitionState)));
            }
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestTime.Data))
            {
                // 16:58
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x24, 0x01, 0x00, 0x31, 0x36, 0x3A, 0x34, 0x38, 0x20, 0x20));
            }
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestDate.Data))
            {
                // 06/26/2016"
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x24, 0x02, 0x00, 0x30, 0x36, 0x2F, 0x32, 0x36, 0x2F, 0x32, 0x30, 0x31, 0x36));
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
            Manager.Instance.EnqueueMessage(message);
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

        private static byte GetIgnitionStateByte(IgnitionState ignitionState)
        {
            switch (ignitionState)
            {
                case IgnitionState.Off:
                    return 0x00;
                case IgnitionState.Acc:
                    return 0x01;
                case IgnitionState.Ign:
                    return 0x03;
                case IgnitionState.Starting:
                    return 0x07;
                default:
                    return 0x00;
            }
        }
    }
}
