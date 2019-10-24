using System;
using Microsoft.SPOT;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;

namespace imBMW.Tools
{
    public static class Logger
    {
        public static event LoggerEventHangler Logged;

        public static void Log(LogPriority priority, string message, string priorityTitle = null)
        {
            var e = Logged;
            if (e != null)
            {
                var args = new LoggerArgs(priority, message, priorityTitle);
                e(args);
            }
        }

        public static void Log(LogPriority priority, Exception exception, string message = null, string priorityTitle = null)
        {
            if (Logged == null)
            {
                return;
            }
            message = exception.Message + (message != null ? " (" + message + ")" : String.Empty) + ". Stack trace: \n" + exception.StackTrace;
            Log(priority, message, priorityTitle);
        }

        public static void FreeMemory()
        {
#if DebugOnRealDeviceOverFTDI && !OnBoardMonitorEmulator
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Microsoft.SPOT.Debug.Print("Free memory:" + Microsoft.SPOT.Debug.GC(true).ToString());
            }
#endif
        }

        public static void Debug(string message, string priorityTitle = null)
        {
            Log(LogPriority.Debug, message, priorityTitle);
        }

        public static void Trace(string message, string priorityTitle = null)
        {
            Log(LogPriority.Trace, message, priorityTitle);
        }

        public static void Trace(Message message, string priorityTitle = null)
        {
            Log(LogPriority.Trace, message.ToPrettyString(true), priorityTitle);
        }

        public static void Info(string message, string priorityTitle = null)
        {
            Log(LogPriority.Info, message, priorityTitle);
        }

        public static void Info(Message message, string priorityTitle = null)
        {
            Log(LogPriority.Info, message.ToPrettyString(true), priorityTitle);
        }

        public static void Warning(string message, string priorityTitle = null)
        {
            Log(LogPriority.Warning, message, priorityTitle);
        }

        public static void Error(string message, string priorityTitle = null)
        {
            Log(LogPriority.Error, message, priorityTitle);
            Radio.DisplayText(message);
        }

        public static void Error(Exception exception, string message = null, string priorityTitle = null)
        {
            Log(LogPriority.Error, exception, message, priorityTitle);
            Radio.DisplayText(message);
        }

        public static void ErrorWithoutLogging(string message)
        {
            Radio.DisplayText(message);
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Microsoft.SPOT.Debug.Print(message);
            }
        }

        public static void Print(string message)
        {
            Microsoft.SPOT.Debug.Print(message);
            FreeMemory();
        }
    }
}
