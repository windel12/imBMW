using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace imBMW.Devices.V2
{
    class LedBlinkingItem
    {
        internal LedBlinkingItem(OutputPort led, byte blinkingCount, ushort interval)
        {
            Led = led;
            BlinkingCount = blinkingCount;
            Interval = interval;
        }

        internal OutputPort Led;
        internal byte BlinkingCount;
        internal ushort Interval;
    }
}
