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

        public static void Debug(string message, string priorityTitle = "DEBUG")
        {
            Log(LogPriority.Debug, message, priorityTitle);
        }

        public static void Trace(string message, string priorityTitle = "TRACE")
        {
            Log(LogPriority.Trace, message, priorityTitle);
        }

        public static void Trace(Message message, string priorityTitle = "TRACE")
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

        public static void Warning(string message, string priorityTitle = "WARN ")
        {
            Log(LogPriority.Warning, message, priorityTitle);
            if (Manager.Instance.Inited)
                InstrumentClusterElectronics.ShowNormalTextWithoutGong(message, timeout: 5000);
        }

        public static void Error(string message, string priorityTitle = "ERROR")
        {
            Log(LogPriority.Error, message, priorityTitle);
            if (Manager.Instance.Inited)
                InstrumentClusterElectronics.ShowNormalTextWithGong(message, timeout: 10000);
        }

        public static void Error(Exception exception, string message = null, string priorityTitle = "ERROR")
        {
            Log(LogPriority.Error, exception, message, priorityTitle);
            if (Manager.Instance.Inited)
                InstrumentClusterElectronics.ShowNormalTextWithGong(message, timeout: 10000);
        }

        public static void ErrorWithoutLogging(string message)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Microsoft.SPOT.Debug.Print(message);
            }
            InstrumentClusterElectronics.ShowNormalTextWithGong(message, timeout: 10000);
        }

        public static void Print(string message)
        {
            Microsoft.SPOT.Debug.Print(message);
            FreeMemory();
        }
    }
}
