using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public delegate void LoggerEventHangler(LoggerArgs args);

    public enum LogPriority
    {
        Debug = 5,
        Trace = 4,
        Info = 3,
        Warning = 2,
        Error = 1,
        FatalError = 0
    }

    public class LoggerArgs
    {
        public readonly DateTime Timestamp;
        public readonly String Message;
        public readonly String LogString;
        public readonly LogPriority Priority;
        public readonly String PriorityTitle;

        public LoggerArgs(LogPriority priority, string message, string priorityTitle = null)
        {
            Timestamp = DateTime.Now;
            Priority = priority;
            Message = message;
            if (priorityTitle != null)
            {
                PriorityTitle = priorityTitle;
            }
            else
            {
                switch (priority)
                {
                    case LogPriority.FatalError:
                        PriorityTitle = "FATAL";
                        break;
                    case LogPriority.Error:
                        PriorityTitle = "ERROR";
                        break;
                    case LogPriority.Warning:
                        PriorityTitle = "warn";
                        break;
                    case LogPriority.Info:
                        PriorityTitle = "i";
                        break;
                }
            }
            LogString = Timestamp.ToString("dd-MM HH:mm:ss.fff") + " (Free memory: " + Debug.GC(true).ToString("D5") + ") [" + PriorityTitle + "] " + message;
        }
    }

}
