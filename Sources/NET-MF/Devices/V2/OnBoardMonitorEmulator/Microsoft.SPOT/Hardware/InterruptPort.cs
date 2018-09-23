using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SPOT.Hardware;

namespace Microsoft.SPOT.Hardware
{
    public sealed class InterruptPort : InputPort
    {
        public InterruptPort(Cpu.Pin portId, bool glitchFilter, ResistorMode resistor, InterruptMode interrupt)
            :base(portId, glitchFilter, resistor)
        {
        }
    }
}
