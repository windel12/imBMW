using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SPOT.Hardware;

namespace Microsoft.SPOT.Hardware
{
    public class InputPort : Port
    {
        public InputPort(Cpu.Pin portId, bool glitchFilter, ResistorMode resistor)
        {
        }

        public void Write(bool state) { }
    }
}
