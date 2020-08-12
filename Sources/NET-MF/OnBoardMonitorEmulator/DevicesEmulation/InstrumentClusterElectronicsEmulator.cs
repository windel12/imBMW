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

        private static float _consumption1;
        public static float Consumption1
        {
            get { return _consumption1; }
            set
            {
                if (_consumption1 == 0)
                {
                    _consumption1 = value;
                }
                else
                {
                    _consumption1 = value;
                    SendConsumption1();
                }
            }
        }

        private static float _consumption2;
        public static float Consumption2
        {
            get { return _consumption2; }
            set
            {
                if (_consumption2 == 0)
                {
                    _consumption2 = value;
                }
                else
                {
                    _consumption2 = value;
                    SendConsumption2();
                }
            }
        }

        public delegate void ShowOBCMessageEventHandler(string message);
        public static event ShowOBCMessageEventHandler OBCTextChanged;

        private static IgnitionState _ignitionState = IgnitionState.Off;
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

            InstrumentClusterElectronics.IgnitionStateChanged += (e) =>
            {
                if (e.PreviousIgnitionState == IgnitionState.Acc && e.CurrentIgnitionState == IgnitionState.Ign)
                {
                    StartAnnounce();
                }

                if (e.PreviousIgnitionState == IgnitionState.Ign && e.CurrentIgnitionState == IgnitionState.Acc)
                {
                    StopAnnounce();
                }
            };
        }

        public static void StartAnnounce()
        {
            if (rpmSpeedAnounceTimer == null)
            {
                rpmSpeedAnounceTimer = new Timer((state) =>
                    {
                        var random = new Random();
                        CurrentSpeed++; // (byte)random.Next(0, 160);
                        CurrentRPM = (ushort)random.Next(800, 4400);

                        var rpmSpeedMessage = new Message(DeviceAddress.InstrumentClusterElectronics,
                            DeviceAddress.GlobalBroadcastAddress, 0x18, (byte)(CurrentSpeed / 2),
                            (byte)(CurrentRPM / 100));

                        Manager.Instance.EnqueueMessage(rpmSpeedMessage);

                        if (KBusManager.Instance.Inited)
                            KBusManager.Instance.EnqueueMessage(rpmSpeedMessage);

                    }, null, 0, rpmSpeedAnounceTimerInterval * 1000);
            }

            if (temperatureAnounceTimer == null)
            {
                temperatureAnounceTimer = new Timer((state) =>
                    {
                        TemperatureCoolant += 5;

                        var temperatureMessage = new Message(DeviceAddress.InstrumentClusterElectronics,
                            DeviceAddress.GlobalBroadcastAddress, 0x19, TemperatureOutside, TemperatureCoolant, 0x00);

                        Manager.Instance.EnqueueMessage(temperatureMessage);

                        if (KBusManager.Instance.Inited)
                            KBusManager.Instance.EnqueueMessage(temperatureMessage);

                    }, null, 0, temperatureAnounceTimerIterval * 1000);
            }
        }

        public static void StopAnnounce()
        {
            if (rpmSpeedAnounceTimer != null)
                rpmSpeedAnounceTimer.Dispose();
            if (temperatureAnounceTimer != null)
                temperatureAnounceTimer.Dispose();
        }

        public static void ProcessMessageToIKE(Message m)
        {
            if (m.Data[0] == 0x1A || m.Data[0] == 0x23)
            {
                string message = ASCIIEncoding.GetString(m.Data.Skip(3));
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
                var hour = DateTime.Now.Hour.ToString("D2");
                var minute = DateTime.Now.Minute.ToString("D2");
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x24, 0x01, 0x00,
                    Convert.ToByte(hour[0]), Convert.ToByte(hour[1]), 0x3A,
                    Convert.ToByte(minute[0]), Convert.ToByte(minute[1]), 0x20, 0x20));
            }
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestDate.Data))
            {
                var day = DateTime.Now.Day.ToString("D2");
                var month = DateTime.Now.Month.ToString("D2");
                var year = DateTime.Now.Year.ToString("D4");
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x24, 0x02, 0x00,
                    Convert.ToByte(month[0]), Convert.ToByte(month[1]), 0x2F,
                    Convert.ToByte(day[0]), Convert.ToByte(day[1]), 0x2F,
                    Convert.ToByte(year[0]), Convert.ToByte(year[1]), Convert.ToByte(year[2]), Convert.ToByte(year[3])));
            }
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestConsumtion1.Data))
            {
                SendConsumption1();
            }
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestConsumtion2.Data))
            {
                SendConsumption2();
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

        private static void SendConsumption1()
        {
            var cons1MessageData = new byte[] { 0x24, 0x04, 0x00 };
            var value = (Consumption1 < 10 ? "0" : "") + $"{Consumption1:F1} l/100";
            var cons1 = Encoding.ASCII.GetBytes(string.Format("{000:000}", value));
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, cons1MessageData.Combine(cons1)));
        }

        private static void SendConsumption2()
        {
            var cons2MessageData = new byte[] { 0x24, 0x05, 0x00 };
            var value = (Consumption2 < 10 ? "0" : "") + $"{Consumption2:F1} l/100";
            var cons2 = Encoding.ASCII.GetBytes(string.Format("{000:000}", value));
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, cons2MessageData.Combine(cons2)));
        }
    }
}
