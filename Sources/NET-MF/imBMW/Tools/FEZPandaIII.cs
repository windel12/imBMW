using Microsoft.SPOT.Hardware;

namespace imBMW.Tools
{
    public static class FEZPandaIII
    {
        public static class Gpio
        {
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin A0 = Cpu.Pin.GPIO_Pin2;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin A1 = Cpu.Pin.GPIO_Pin3;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin A2 = Cpu.Pin.GPIO_Pin4;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin A3 = Cpu.Pin.GPIO_Pin5;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin A4 = Cpu.Pin.GPIO_Pin6;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin A5 = Cpu.Pin.GPIO_Pin7;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D0 = Cpu.Pin.GPIO_Pin10;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D1 = Cpu.Pin.GPIO_Pin9;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D10 = Cpu.Pin.GPIO_Pin15;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D11 = (Cpu.Pin)21;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D12 = (Cpu.Pin)20;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D13 = (Cpu.Pin)19;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D2 = (Cpu.Pin)23;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            public const Cpu.Pin D20 = (Cpu.Pin)66;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D21 = (Cpu.Pin)27;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D22 = (Cpu.Pin)69;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D23 = (Cpu.Pin)32;
            //            //
            //            // Summary:
            //            //     GPIO pin.
                        public const Cpu.Pin D24 = (Cpu.Pin)70;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D25 = (Cpu.Pin)33;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            public const Cpu.Pin D26 = Cpu.Pin.GPIO_Pin13;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D27 = (Cpu.Pin)36;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D28 = Cpu.Pin.GPIO_Pin14;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D29 = (Cpu.Pin)37;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D3 = (Cpu.Pin)22;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D30 = (Cpu.Pin)28;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D31 = Cpu.Pin.GPIO_Pin1;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D32 = (Cpu.Pin)29;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D33 = Cpu.Pin.GPIO_Pin0;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D34 = (Cpu.Pin)18;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D35 = (Cpu.Pin)26;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D36 = (Cpu.Pin)34;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D37 = (Cpu.Pin)54;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D38 = (Cpu.Pin)35;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D39 = (Cpu.Pin)53;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D4 = (Cpu.Pin)48;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D40 = (Cpu.Pin)57;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D41 = (Cpu.Pin)51;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D42 = (Cpu.Pin)56;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D43 = (Cpu.Pin)52;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D44 = (Cpu.Pin)59;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D45 = (Cpu.Pin)71;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D46 = (Cpu.Pin)60;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D47 = (Cpu.Pin)72;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D48 = (Cpu.Pin)38;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D49 = (Cpu.Pin)64;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D5 = (Cpu.Pin)25;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D50 = (Cpu.Pin)39;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D51 = (Cpu.Pin)65;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D52 = Cpu.Pin.GPIO_Pin8;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D6 = (Cpu.Pin)24;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin D7 = (Cpu.Pin)49;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D8 = (Cpu.Pin)17;
            //
            // Summary:
            //     GPIO pin.
            public const Cpu.Pin D9 = (Cpu.Pin)16;
            //
            // Summary:
            //     The Cpu.Pin for the LDR0 button.
            public const Cpu.Pin Ldr0 = (Cpu.Pin)67;
            //
            // Summary:
            //     The Cpu.Pin for the LDR1 button.
            public const Cpu.Pin Ldr1 = (Cpu.Pin)68;
            //
            // Summary:
            //     The Cpu.Pin for LED 1.
            public const Cpu.Pin Led1 = (Cpu.Pin)78;
            //
            // Summary:
            //     The Cpu.Pin for LED 2.
            public const Cpu.Pin Led2 = (Cpu.Pin)77;
            //
            // Summary:
            //     The Cpu.Pin for LED 3.
            public const Cpu.Pin Led3 = (Cpu.Pin)75;
            //
            // Summary:
            //     The Cpu.Pin for LED 4.
            public const Cpu.Pin Led4 = (Cpu.Pin)73;
            //            //
            //            // Summary:
            //            //     GPIO pin.
            //            public const Cpu.Pin Mod = (Cpu.Pin)79;
            //            //
            //            // Summary:
            //            //     The SD card detect pin.
            //            public const Cpu.Pin SdCardDetect = (Cpu.Pin)58;
        }
    }
}
