using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHI.Processor
{
    public static class Watchdog
    {
        public static ResetCause LastResetCause { get; }

        public static void Enable(int timeout)
        {
        }

        public static void ResetCounter()
        {
        }

        public enum ResetCause : byte
        {
            //
            // Summary:
            //     The system was not reset due to watchdog.
            Normal = 0,
            //
            // Summary:
            //     The system was reset due to watchdog.
            Watchdog = 1
        }
    }
}
