using System;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    public static class HeadlightVerticalAimControl
    {
        static double frontSensorVoltage;
        static double rearSensorVoltage;

        static HeadlightVerticalAimControl()
        {
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.HeadlightVerticalAimControl, DeviceAddress.Diagnostic, ProcessDiagMessageFromHeadlightVerticalAimControl);
        }

        static void ProcessDiagMessageFromHeadlightVerticalAimControl(Message m)
        {
            if (m.Data.Length == 13 && m.Data[0] == 0xA0) // A0 88 37 59 64 D1 03 01 05 43 00 07 05
            {
                m.ReceiverDescription = "LWR2A.PRG -> IDENT Response";
                FrontSensorVoltage = 0.01;
            }
            if (m.Data.Length == 3 && m.Data[0] == 0xA0) // A0 58 B2
            {
                m.ReceiverDescription = "LWR2A.PRG -> STATUS_SENSOR_LESEN Response";

                FrontSensorVoltage = (float)m.Data[1] / 255 * 5;
                RearSensorVoltage = (float)m.Data[2] / 255 * 5;
            }
        }

        public static double FrontSensorVoltage
        {
            get { return frontSensorVoltage; }
            private set
            {
                frontSensorVoltage = value;

                var e = FrontSensorVoltageChanged;
                if (e != null)
                {
                    e(value);
                }
            }
        }

        public static double RearSensorVoltage
        {
            get { return rearSensorVoltage; }
            private set
            {
                rearSensorVoltage = value;

                var e = RearSensorVoltageChanged;
                if (e != null)
                {
                    e(value);
                }
            }
        }

        public static void IDENT()
        {
            var ident = new Message(DeviceAddress.Diagnostic, DeviceAddress.HeadlightVerticalAimControl, 0x00);
            KBusManager.Instance.EnqueueMessage(ident);
        }

        public static void STATUS_LESSEN()
        {
            var status_sensor_lessen = new Message(DeviceAddress.Diagnostic, DeviceAddress.HeadlightVerticalAimControl, 0x0B);
            KBusManager.Instance.EnqueueMessage(status_sensor_lessen);
        }

        public static void STATUS_SENSOR_LESSEN()
        {
            var status_sensor_lessen = new Message(DeviceAddress.Diagnostic, DeviceAddress.HeadlightVerticalAimControl, 0x0C);
            KBusManager.Instance.EnqueueMessage(status_sensor_lessen);
        }

        public static void STEUERN_ANTRIEBE()
        {
            var steuern_antriebe = new DS2Message(DeviceAddress.HeadlightVerticalAimControl, 0x1C, 0x00, 0x00, 0x00, 0xFF);
            var kbusMessage = steuern_antriebe.ToIKBusMessage();
            KBusManager.Instance.EnqueueMessage(kbusMessage);
        }

        public static void DIAGNOSE_ENDE()
        {
            var diagnose_ende = new Message(DeviceAddress.Diagnostic, DeviceAddress.HeadlightVerticalAimControl, 0x9F);
            KBusManager.Instance.EnqueueMessage(diagnose_ende);
        }

        public static event ActionDouble FrontSensorVoltageChanged;
        public static event ActionDouble RearSensorVoltageChanged;
    }
}
