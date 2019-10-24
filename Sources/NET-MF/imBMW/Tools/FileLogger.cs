#if !MF_FRAMEWORK_VERSION_V4_1

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using imBMW.iBus.Devices.Real;
using Debug = Microsoft.SPOT.Debug;

namespace imBMW.Tools
{
    public class FileLogger
    {
        const byte flushLines = 5;

        static byte queueLimit = 100;

        static ushort unflushed = 0;

        static StreamWriter writer;
        static QueueThreadWorker queue;
        static Action FlushCallback;

        private static bool queueLimitExceeded;

        public static void Create()
        {
            if (queue == null)
            {
                queue = new QueueThreadWorker(ProcessItem, "fileLoggerThread", ThreadPriority.Lowest, true);
            }

            Logger.Logged += Logger_Logged;
        }

        public static void Init(string path, Action flushCallback = null)
        {
            try
            {
                FlushCallback = flushCallback;

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fullpath;
                ushort i = 0;
                do
                {
                    fullpath = path + @"\traceLog" + (i++ == 0 ? "" : i.ToString()) + ".log";
                } while (File.Exists(fullpath));

                writer = new StreamWriter(fullpath, append:true);

                queue.Start();
            }
            catch (Exception ex)
            {
                FrontDisplay.RefreshLEDs(LedType.Red);
                Logger.Error(ex, "file logger init");
            }
        }

        static void Logger_Logged(LoggerArgs args)
        {
            if (queue.Count < queueLimit || args.Priority == LogPriority.Debug || args.Priority == LogPriority.Error)
            {
                queue.Enqueue(args.LogString);

                if (queue.Count < queueLimit - 10)
                    queueLimitExceeded = false;
            }
            if(queue.Count == queueLimit && !queueLimitExceeded)
            {
                queueLimitExceeded = true;
                Logger.Debug("Queue is full");
            }
            
#if (NETMF || OnBoardMonitorEmulator) && DebugOnRealDeviceOverFTDI
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
                    //if (FlushCallback != null)
                    //{
                    //    FlushCallback();
                    //}
                    Debug.GC(true);
                    unflushed = 0;
                }

                Thread.Sleep(0);
            }
            catch (Exception ex)
            {
                // don't use logger to prevent recursion
                Logger.ErrorWithoutLogging("Can't write log to sd: " + ex.Message);
            }
        }

        public static void Eject()
        {
            Logger.Logged -= Logger_Logged;
            queue.Clear();
        }

        public static void Dispose(int waitTimeout = 2000)
        {
            Logger.Logged -= Logger_Logged;
            bool waitResult = queue.WaitTillQueueBeEmpty(waitTimeout);

            writer.Flush();
            if (FlushCallback != null)
            {
                FlushCallback();
            }
            Debug.GC(true);
            unflushed = 0;

            writer.Dispose();
        }
    }
}

#endif