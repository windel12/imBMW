using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GHI.Processor
{
    public static class Watchdog
    {
        private static int _counter;
        private static int _timeout;
        private static Timer _watchdogTimer;

        public static ResetCause LastResetCause { get; }

        public static void Enable(int timeout)
        {
            _timeout = timeout;
            _counter = timeout;
            _watchdogTimer = new Timer((arg) =>
            {
                _counter -= 100;
                if (_counter <= 0)
                {
                    System.Windows.MessageBox.Show("Watchdog reset occured!!!");
                }
            }, null, 0, 100);
        }

        public static void ResetCounter()
        {
            _counter = _timeout;
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
