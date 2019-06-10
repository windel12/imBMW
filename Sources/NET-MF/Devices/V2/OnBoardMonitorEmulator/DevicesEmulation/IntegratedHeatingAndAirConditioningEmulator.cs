using System;
using System.Threading;
using imBMW.iBus;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class IntegratedHeatingAndAirConditioningEmulator
    {
        public static Timer AirConditioningCompressorStatusAnounceTimer;

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
