using System;
using Microsoft.SPOT.Hardware;

namespace GHI.IO
{
    //
    // Summary:
    //     Allows a high frequency signal to be generated on a given digital pin. See https://www.ghielectronics.com/docs/24/
    //     for more information.
    //
    // Remarks:
    //     Software generation is used so accuracy may suffer and is platform dependent.
    public class SignalGenerator
    {
        public SignalGenerator(Cpu.Pin pin, bool initialValue)
        {
        }

        public void Set(bool initialValue, uint[] timingsBuffer, bool repeat)
        { }

        public void Set(bool initialValue, uint[] timingsBuffer, int bufferOffset, int bufferCount, bool repeat)
        { }
    }
}
