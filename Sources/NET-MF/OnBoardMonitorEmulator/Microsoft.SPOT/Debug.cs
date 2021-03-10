using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SPOT
{
    public static class Debug
    {
        public static void EnableGCMessages(bool enable) { }

        public static uint GC(bool force)
        {
            return (uint)System.GC.GetTotalMemory(true);
        }

        public static void Print(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}
