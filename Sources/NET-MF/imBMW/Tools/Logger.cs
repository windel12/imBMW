using System;
using Microsoft.SPOT;
using imBMW.iBus;

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
#if DebugOnRealDeviceOverFTDI
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Debug.Print("Free memory:" + Debug.GC(true));
            }
#endif
        }

        public static void TryTrace(string message, string priorityTitle = null)
        {
            try
            {
                Trace(message, priorityTitle);
            }
            catch (Exception ex) { }
        }

        public static void TryTrace(Message message, string priorityTitle = null)
        {
            try
            {
                Trace(message, priorityTitle);
            }
            catch (Exception ex) { }
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

        public static void Warning(Message message, string priorityTitle = null)
        {
            Log(LogPriority.Warning, message.ToPrettyString(true), priorityTitle);
        }

        public static void Warning(Exception exception, string message = null, string priorityTitle = null)
        {
            Log(LogPriority.Warning, exception, message, priorityTitle);
        }

        public static void TryError(string message, string priorityTitle = null)
        {
            try
            {
                Error(message, priorityTitle);
            }
            catch (Exception ex) { }
        }

        public static void TryError(Exception exception, string message = null, string priorityTitle = null)
        {
            try
            {
                Error(exception, message, priorityTitle);
            }
            catch (Exception ex) { }
        }

        public static void Error(string message, string priorityTitle = null)
        {
            Log(LogPriority.Error, message, priorityTitle);
        }

        public static void Error(Exception exception, string message = null, string priorityTitle = null)
        {
            Log(LogPriority.Error, exception, message, priorityTitle);
        }
    }
}
