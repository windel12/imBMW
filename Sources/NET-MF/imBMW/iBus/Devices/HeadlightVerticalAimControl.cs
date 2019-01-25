//using System;

//namespace imBMW.iBus.Devices.Real
//{
//    public static class HeadlightVerticalAimControl
//    {
//        static double frontSensorVoltage;
//        static double rearSensorVoltage;

//        static HeadlightVerticalAimControl()
//        {
//            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.HeadlightVerticalAimControl, DeviceAddress.Diagnostic, ProcessDiagMessageFromHeadlightVerticalAimControl);
//        }

//        static void ProcessDiagMessageFromHeadlightVerticalAimControl(Message m)
//        {
//            if (m.Data.Length == 3 && m.Data[0] == 0xA0) // 0xA0 - DIAG data
//            {
//                m.ReceiverDescription = "LWR2A.PRG -> STATUS_SENSOR_LESEN - Response";

//                FrontSensorVoltage = (float)m.Data[1] / 255 * 5;
//                RearSensorVoltage = (float)m.Data[2] / 255 * 5;
//            }
//        }

//        public static double FrontSensorVoltage
//        {
//            get { return frontSensorVoltage; }
//            private set
//            {
//                frontSensorVoltage = value;

//                var e = FrontSensorVoltageChanged;
//                if (e != null)
//                {
//                    e(value);
//                }
//            }
//        }

//        public static double RearSensorVoltage
//        {
//            get { return rearSensorVoltage; }
//            private set
//            {
//                rearSensorVoltage = value;

//                var e = RearSensorVoltageChanged;
//                if (e != null)
//                {
//                    e(value);
//                }
//            }
//        }

//        public static void STATUS_SENSOR_LESSEN()
//        {
//            var status_sensor_lessen = new DS2Message(DeviceAddress.HeadlightVerticalAimControl, 0x0C);
//            var kbusMessage = status_sensor_lessen.ToIKBusMessage();
//            KBusManager.Instance.EnqueueMessage(kbusMessage);
//        }

//        public static void STEUERN_ANTRIEBE()
//        {
//            var steuern_antriebe = new DS2Message(DeviceAddress.HeadlightVerticalAimControl, 0x1C, 0x00, 0x00, 0x00, 0xFF);
//            var kbusMessage = steuern_antriebe.ToIKBusMessage();
//            KBusManager.Instance.EnqueueMessage(kbusMessage);
//        }

//        public static void DIAGNOSE_ENDE()
//        {
//            var diagnose_ende = new DS2Message(DeviceAddress.HeadlightVerticalAimControl, 0x9F);
//            var kbusMessage = diagnose_ende.ToIKBusMessage();
//            KBusManager.Instance.EnqueueMessage(kbusMessage);
//        }

//        public static event VoltageEventHandler FrontSensorVoltageChanged;
//        public static event VoltageEventHandler RearSensorVoltageChanged;
//    }
//}
