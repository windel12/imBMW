using System;
using System.Collections;

namespace imBMW.Tools
{
    public class ErrorIdentifier
    {
        public static byte SleepModeFlowBrokenErrorId = 0x00;
        public static byte UknownError = 0xFF;

        public static Hashtable ErrorDescriptions;

        static ErrorIdentifier()
        {
            ErrorDescriptions = new Hashtable();
            ErrorDescriptions.Add(SleepModeFlowBrokenErrorId, "Sleep mode flow was broken");
            ErrorDescriptions.Add(UknownError, "Unknown error");
        }
    }
}
