using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SPOT.Hardware
{
    public delegate void NativeEventHandler(uint data1, uint data2, DateTime time);

    public class NativeEventDispatcher : IDisposable
    {
        public event NativeEventHandler OnInterrupt;

        public virtual void Dispose() { }

        protected virtual void Dispose(bool disposing) { }
    }
}
