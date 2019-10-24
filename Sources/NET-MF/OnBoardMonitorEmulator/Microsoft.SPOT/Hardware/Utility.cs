using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SPOT.Hardware
{
    public static class Utility
    {
        public static DateTime CurrentDateTime;

        public static void SetLocalTime(DateTime dateTime)
        {
            CurrentDateTime = dateTime;
        }

        public static TimeSpan GetMachineTime()
        {
            return new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        }
    }
}
