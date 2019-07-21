using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SPOT.Hardware
{
    public class Port : NativeEventDispatcher
    {
        public bool Read() { return true; }

        public enum InterruptMode
        {
            InterruptNone = 0,
            InterruptEdgeLow = 1,
            InterruptEdgeHigh = 2,
            InterruptEdgeBoth = 3,
            InterruptEdgeLevelHigh = 4,
            InterruptEdgeLevelLow = 5
        }
        public enum ResistorMode
        {
            Disabled = 0,
            PullDown = 1,
            PullUp = 2
        }
    }
}
