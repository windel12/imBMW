using System;

namespace imBMW.Diagnostics.DME
{
    public class DDEAnalogValues
    {
        /// <summary>
        /// Time of log entry
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Since logger started
        /// </summary>
        public TimeSpan TimeSpan { get; set; }

        public DDEAnalogValues(DateTime loggerStarted)
        {
            Time = DateTime.Now;
            TimeSpan = Time - loggerStarted;
        }

        public DDEAnalogValues()
        {
            Time = DateTime.Now;
        }

        public float AtmosphericPressure { get; set; }

        public int RPM { get; set; }

        public float RailPressureIst { get; set; }

        public float RailPressureSoll { get; set; }

        public float ChargeAirPressureIst { get; set; }

        public float ChargeAirPressureSoll { get; set; }

        public float LuftMasseIst { get; set; }

        public float LuftMasseSoll { get; set; }

        public override string ToString()
        {
            return string.Concat(Time, " RPM:", RPM);
        }
    }
}
