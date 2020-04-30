using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;

namespace imBMW.Devices.V2.Hardware
{
    public class Pin
    {
        public static Cpu.Pin TH3122SENSTA = FEZPandaIII.Gpio.D22; //Generic.GetPin('C', 2);

        //public static Cpu.Pin D_BUS_TH3122SENSTA = FEZPandaIII.Gpio.D32; 

        public static Cpu.Pin K_BUS_TH3122SENSTA = FEZPandaIII.Gpio.D41; 

        public static Cpu.Pin ResetPin = FEZPandaIII.Gpio.D20; //Generic.GetPin('C', 2);
    }
}
