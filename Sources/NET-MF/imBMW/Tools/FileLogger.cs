#if !MF_FRAMEWORK_VERSION_V4_1

using System;
using Microsoft.SPOT;
using System.IO;
using System.Threading;
using imBMW.iBus.Devices.Real;

namespace imBMW.Tools
{
    public class FileLogger
    {
        const byte flushLines = 5;

        static ushort unflushed = 0;

        static StreamWriter writer;
        static QueueThreadWorker queue;
        public static Action FlushCallback;

        public static void Init(string path, Action flushCallback = null)
        {
            try
            {
                FileLogger.FlushCallback = flushCallback;

                Logger.FreeMemory();

                queue = new QueueThreadWorker(ProcessItem, "fileLoggerThread", ThreadPriority.Lowest);

                Logger.FreeMemory();

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                Logger.FreeMemory();

                string fullpath;
                ushort i = 0;
                do
                {
                    fullpath = path + @"\traceLog" + (i++ == 0 ? "" : i.ToString()) + ".log";
                    Logger.FreeMemory();
                } while (File.Exists(fullpath));

                Logger.FreeMemory();

                writer = new StreamWriter(fullpath, append:true);

                Logger.FreeMemory();

                Logger.Logged += Logger_Logged;

                Logger.Info("File logger path: " + fullpath);
            }
            catch (Exception ex)
            {
                FrontDisplay.RefreshLEDs(LedType.RedBlinking);
                Logger.Error(ex, "file logger init");
            }
        }

        static void Logger_Logged(LoggerArgs args)
        {
            queue.Enqueue(args.LogString);
#if DebugOnRealDeviceOverFTDI
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Debug.Print(args.LogString);
                Logger.FreeMemory();
            }
#endif
        }

        static void ProcessItem(object o)
        {
            try
            {
                writer.WriteLine((string)o);
                o = null;
                if (++unflushed == flushLines)
                {
                    writer.Flush();
                    if (flushCallback != null)
                    {
                        flushCallback();
                    }
                    Debug.GC(true);
                    unflushed = 0;
                }

                Thread.Sleep(0);
            }
            catch (Exception ex)
            {
                // don't use logger to prevent recursion
                Logger.Log(LogPriority.Info, "Can't write log to sd: " + ex.Message);
            }
        }
    }
}

#endif