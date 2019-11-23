using System;
using imBMW.iBus;
using imBMW.Tools;
using imBMW.Enums;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class IntegratedHeatingAndAirConditioningEmulator
    {
        internal static byte CodingData1 { get; set; } = 0x78;
        internal static byte CodingData2 { get; set; } = 0x8C;
        internal static byte CodingData3 { get; set; } = 0x28;
        internal static byte CodingData4 { get; set; } = 0x70;

        public static void Init()
        {
        }

        static IntegratedHeatingAndAirConditioningEmulator()
        {
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Diagnostic, DeviceAddress.IntegratedHeatingAndAirConditioning, ProcessMessageFromDiagnostic);
        }

        public static void ProcessMessageFromDiagnostic(Message m)
        {
            if (m.Data[0] == 0x08) // Read coding data
            {
                KBusManager.Instance.EnqueueMessage(new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.Diagnostic, 
                    0xA0, CodingData1, CodingData2, CodingData3, CodingData4));
            }
            if (m.Data[0] == 0x09 && m.Data.Length == 10) // Write coding data;
            {
                CodingData1 = m.Data[6];
                CodingData2 = m.Data[7];
                CodingData3 = m.Data[8];
                CodingData4 = m.Data[9];
            }
        }

        public static Message GetAirConditioningCompressorStatusMessage(byte firstByte, byte secondByte)
        {
            return new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.InstrumentClusterElectronics, 0x83, firstByte, secondByte);
        }

        private static byte _airConditioningCompressorStatus_FirstByte = 0xD0;
        public static byte AirConditioningCompressorStatus_FirstByte
        {
            get { return _airConditioningCompressorStatus_FirstByte; }
            set
            {
                _airConditioningCompressorStatus_FirstByte = value;
                var message = GetAirConditioningCompressorStatusMessage(value, AirConditioningCompressorStatus_SecondByte);
                if (KBusManager.Instance.Inited)
                    KBusManager.Instance.EnqueueMessage(message);
            }
        }

        private static byte _airConditioningCompressorStatus_SecondByte = 0x00;
        public static byte AirConditioningCompressorStatus_SecondByte
        {
            get { return _airConditioningCompressorStatus_SecondByte; }
            set
            {
                _airConditioningCompressorStatus_SecondByte = value;
                var message = GetAirConditioningCompressorStatusMessage(AirConditioningCompressorStatus_FirstByte, value);
                if (KBusManager.Instance.Inited)
                    KBusManager.Instance.EnqueueMessage(message);
            }
        }

        //public static void StartAnounce()
        //{
        //    AirConditioningCompressorStatusAnounceTimer = new Timer((state) =>
        //        {
        //            if (KBusManager.Instance.Inited)
        //                KBusManager.Instance.EnqueueMessage(rpmSpeedMessage);
        //        }, null, 0, 2);
        //}
    }
}
