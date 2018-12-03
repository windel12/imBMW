using System.Threading;

namespace imBMW.iBus.Devices.Real
{
    public enum AuxilaryHeaterStatus
    {
        Stopped,
        StopPending,
        StartPending,
        Starting,
        Started
    }

    public static class AuxilaryHeater
    {
        /// <summary> 6B 04 9E F1 </summary>
        internal static DS2Message SteuernZuheizerOn1 = new DS2Message(DeviceAddress.AuxilaryHeater, 0x9E);
        /// <summary> 6B 05 0C E0 82 </summary>
        internal static DS2Message SteuernZuheizerOn2 = new DS2Message(DeviceAddress.AuxilaryHeater, 0x0C, 0xE0);
        /// <summary> 6B 05 0C 00 62 </summary>
        public static DS2Message SteuernZuheizerOff = new DS2Message(DeviceAddress.AuxilaryHeater, 0x0C, 0x00);
        /// <summary> 6B 04 A0 CF </summary>
        public static DS2Message ZuheizerStatusOk = new DS2Message(DeviceAddress.AuxilaryHeater, 0xA0);
        /// <summary> 6B 03 3F A0 F7 </summary>
        public static Message ZuheizerStatusOk_KBus = new Message(DeviceAddress.AuxilaryHeater, DeviceAddress.Diagnostic, 0xA0);

        public static byte[] DataZuheizerStatusRequest = new byte[] { 0x00 };

        private static AuxilaryHeaterStatus status;

        public static AuxilaryHeaterStatus Status
        {
            get { return status; }
            private set
            {
                status = value;

                var e = AuxilaryHeaterStatusChanged;
                if (e != null)
                {
                    e(value);
                }
            }
        }

        static AuxilaryHeater()
        {
            DbusManager.AddMessageReceiverForDestinationDevice(DeviceAddress.AuxilaryHeater, ProcessAuxilaryHeaterMessageFromDBUS);
        }

        public static void ProcessAuxilaryHeaterMessageFromDBUS(Message m)
        {
            if (m.Data[0] == 0xA0)
            {
                Thread.Sleep(100);
                if (Status == AuxilaryHeaterStatus.StartPending)
                {
                    Status = AuxilaryHeaterStatus.Starting;
                    DbusManager.EnqueueMessage(SteuernZuheizerOn2);
                    return;
                }
                if (Status == AuxilaryHeaterStatus.Starting)
                {
                    Status = AuxilaryHeaterStatus.Started;
                    return;
                }
                if (Status == AuxilaryHeaterStatus.StopPending)
                {
                    Status = AuxilaryHeaterStatus.Stopped;
                    return;
                }
            }
        }

        public static void StartAuxilaryHeater()
        {
            DbusManager.EnqueueMessage(SteuernZuheizerOn1);
            Status = AuxilaryHeaterStatus.StartPending;
        }

        public static void StopAuxilaryHeater()
        {
            DbusManager.EnqueueMessage(SteuernZuheizerOff);
            Status = AuxilaryHeaterStatus.StopPending;
        }

        public static void Init() { }

        public delegate void AuxilaryHeaterStatusEventHandler(AuxilaryHeaterStatus status);
        public static event AuxilaryHeaterStatusEventHandler AuxilaryHeaterStatusChanged;
    }
}
