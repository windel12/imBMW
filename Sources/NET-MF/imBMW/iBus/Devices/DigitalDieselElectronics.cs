using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    public static class DigitalDieselElectronics
    {
        // bar
        public static double PresupplyPressure { get; private set; }

        // 1/min
        public static double Rpm { get; private set; }

        // bar
        public static double BoostActual { get; private set; }

        // bar
        public static double BoostTarget { get; private set; }

        // %
        public static double VNT { get; private set; }

        // mm3
        public static double InjectionQuantity { get; private set; }

        // bar
        public static double RailPressureTarget { get; private set; }

        // bar
        public static double RailPressureActual { get; private set; }

        // %
        public static double PressureRegulationValve { get; private set; }

        // kg/h
        public static double AirMass { get; private set; }

        // mg/Hub
        public static double AirMassPerStroke { get; private set; }

        public static byte EluefterFrequency { get; private set; }

        private static byte[] admVDF = {0x20, 0x06};
        private static byte[] dzmNmit = { 0x0F, 0x10 };
        private static byte[] ldmP_Llin = { 0x0F, 0x40 };
        private static byte[] ldmP_Lsoll = { 0x0F, 0x42 };
        private static byte[] ehmFLDS = { 0x0E, 0x81 };
        private static byte[] zumPQsoll = { 0x1F, 0x5E };
        private static byte[] zumP_RAIL = { 0x1F, 0x5D };
        private static byte[] ehmFKDR = { 0x0E, 0xE5 };
        private static byte[] mrmM_EAKT = { 0x0F, 0x80 };
        private static byte[] aroIST_4 = { 0x00, 0x10 };

        private static byte[] armM_List = { 0x0F, 0x30 };
        private static byte[] anmWTF = { 0x0F, 0x00 };

        public static DBusMessage QueryMessage = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10,
            admVDF[0], admVDF[1], 
            dzmNmit[0], dzmNmit[1], 
            ldmP_Llin[0], ldmP_Llin[1], 
            ldmP_Lsoll[0], ldmP_Lsoll[1],
            ehmFLDS[0], ehmFLDS[1],
            zumPQsoll[0], zumPQsoll[1], 
            zumP_RAIL[0], zumP_RAIL[1],
            ehmFKDR[0], ehmFKDR[1],
            mrmM_EAKT[0], mrmM_EAKT[1],
            aroIST_4[0], aroIST_4[1]);

        public static DBusMessage status_motortemperatur = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10, anmWTF[0], anmWTF[1]);
        public static DBusMessage status_vorfoederdruck = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10, admVDF[0], admVDF[1]);

        public static DBusMessage SteuernEluefter(byte value) => new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x30, 0xC7, 0x07, value);

        static DigitalDieselElectronics()
        {
            VolumioManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Volumio, DeviceAddress.imBMW, ProcessFromDDEMessage);
        }

        static void ProcessFromDDEMessage(Message m)
        {
            if (m.Data[0] == 0x6C && m.Data[1] == 0x10)
            {
                Logger.Trace("Response from DDE: " + m.Data.ToHex(' '));
                var d = m.Data;
                if (d.Length > 3)
                {
                    PresupplyPressure = ((d[2] << 8) + d[3]) * 0.001;
                }
                if (d.Length > 5)
                {
                    Rpm = ((d[4] << 8) + d[5]);
                }
                if (d.Length > 7)
                {
                    BoostActual = ((d[6] << 8) + d[7]);
                }
                if (d.Length > 9)
                {
                    BoostTarget = ((d[8] << 8) + d[9]);
                }
                if (d.Length > 11)
                {
                    VNT = ((d[10] << 8) + d[11]) * 0.01;
                }
                if (d.Length > 13)
                {
                    RailPressureTarget = ((d[12] << 8) + d[13]) * 10.235414;
                }
                if (d.Length > 15)
                {
                    RailPressureActual = ((d[14] << 8) + d[15]) * 10.235414;
                }
                if (d.Length > 17)
                {
                    PressureRegulationValve = ((d[16] << 8) + d[17]) * 0.01;
                }
                if (d.Length > 19)
                {
                    InjectionQuantity = ((d[18] << 8) + d[19]) * 0.01;
                }
                if (d.Length > 21)
                {
                    AirMass = ((d[20] << 8) + d[21]) * 0.0359929742;
                }

                //AirMassPerStroke = ((d[18] << 8) + d[19]) * 0.1;
            }

            if (m.Data[0] == 0x70 && m.Data[1] == 0xC7)
            {
                EluefterFrequency = m.Data[3];
            }

            var e = MessageReceived;
            if (e != null)
            {
                e();
            }
        }

        public delegate void EventHandler();

        public static event EventHandler MessageReceived;
    }
}
