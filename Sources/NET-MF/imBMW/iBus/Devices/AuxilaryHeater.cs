using System.Threading;
using imBMW.Enums;

namespace imBMW.iBus.Devices.Real
{
    public static class AuxilaryHeater
    {
        ///// <summary> 6B 04 9E F1 </summary>
        //internal static DS2Message DiagnoseStart = new DS2Message(DeviceAddress.AuxilaryHeater, 0x9E);
        ///// <summary> 6B 05 0C E0 82 </summary>
        //internal static DS2Message SteuernZuheizerOn = new DS2Message(DeviceAddress.AuxilaryHeater, 0x0C, 0xE0);
        ///// <summary> 6B 05 0C 00 62 </summary>
        //public static DS2Message SteuernZuheizerOff = new DS2Message(DeviceAddress.AuxilaryHeater, 0x0C, 0x00);
        ///// <summary> 6B 04 A0 CF </summary>
        //public static DS2Message DiagnoseOk = new DS2Message(DeviceAddress.AuxilaryHeater, 0xA0);
        ///// <summary> 6B 03 3F A0 F7 </summary>
        //public static Message DiagnoseOk_KBus = new Message(DeviceAddress.AuxilaryHeater, DeviceAddress.Diagnostic, 0xA0);


        /// <summary> ZUH > IHKA: 93 00 22 ?? </summary>
        public static Message AuxilaryHeaterWorkingResponse = new Message(DeviceAddress.AuxilaryHeater, DeviceAddress.IntegratedHeatingAndAirConditioning, 0x93, 0x00, 0x22);
        /// <summary> ZUH > IHKA: 93 00 21 ?? </summary>
        public static Message AuxilaryHeaterStopped1 = new Message(DeviceAddress.AuxilaryHeater, DeviceAddress.IntegratedHeatingAndAirConditioning, 0x93, 0x00, 0x21);
        /// <summary> ZUH > IHKA: 93 00 11 ?? </summary>
        public static Message AuxilaryHeaterStopped2 = new Message(DeviceAddress.AuxilaryHeater, DeviceAddress.IntegratedHeatingAndAirConditioning, 0x93, 0x00, 0x11);

        public static byte[] DataZuheizerStatusRequest = new byte[] { 0x00 };

        private static AuxilaryHeaterStatus status;
        public static AuxilaryHeaterStatus Status
        {
            get { return status; }
            internal set
            {
                status = value;

                var e = StatusChanged;
                if (e != null)
                {
                    e(value);
                }
            }
        }

        static AuxilaryHeater()
        {
            //DBusManager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.AuxilaryHeater, ProcessAuxilaryHeaterMessageFromDBUS);
        }

        //public static void ProcessAuxilaryHeaterMessageFromDBUS(Message m)
        //{
        //    if (m.Data[0] == 0xA0)
        //    {
        //        Thread.Sleep(100);
        //        if (Status == AuxilaryHeaterStatus.StartPending)
        //        {
        //            Status = AuxilaryHeaterStatus.Starting;
        //            DBusManager.Instance.EnqueueMessage(SteuernZuheizerOn);
        //            return;
        //        }
        //        if (Status == AuxilaryHeaterStatus.Starting)
        //        {
        //            Status = AuxilaryHeaterStatus.Started;
        //            return;
        //        }
        //        if (Status == AuxilaryHeaterStatus.StopPending)
        //        {
        //            Status = AuxilaryHeaterStatus.Stopped;
        //            return;
        //        }
        //    }
        //}

        //public static void StartAuxilaryHeaterOverDBus()
        //{
        //    DBusManager.Instance.EnqueueMessage(DiagnoseStart);
        //    Status = AuxilaryHeaterStatus.StartPending;
        //}

        //public static void StopAuxilaryHeaterOverDBus()
        //{
        //    DBusManager.Instance.EnqueueMessage(SteuernZuheizerOff);
        //    Status = AuxilaryHeaterStatus.StopPending;
        //}

        public static void Init() { }

        public delegate void AuxilaryHeaterStatusEventHandler(AuxilaryHeaterStatus status);
        public static event AuxilaryHeaterStatusEventHandler StatusChanged;
    }
}
