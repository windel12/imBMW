using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SPOT.Hardware
{
    public class OutputPort : Port
    {
        public OutputPort(Cpu.Pin portId, bool initialState)
        {   
        }

        protected OutputPort(Cpu.Pin portId, bool initialState, bool glitchFilter, ResistorMode resistor)
        {
        }

        public bool InitialState { get; }

        public void Write(bool state) { }
    }
}
