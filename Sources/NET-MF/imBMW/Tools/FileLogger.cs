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

        private static byte queueLimit = 100;
        private static bool queueLimitExceeded = false;

        static ushort unflushed = 0;

        static StreamWriter writer;
        static QueueThreadWorker queue;
        public static Action FlushCallback;

        public static bool Inited = false;

        public static void BaseInit()
        {
            queue = new QueueThreadWorker(ProcessItem, "fileLoggerThread", ThreadPriority.Lowest, true);
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
                Inited = true;
            }
            catch (Exception ex)
            {
                FrontDisplay.RefreshLEDs(LedType.Red);
                Logger.Error(ex, "file logger init");
            }
        }

        static void Logger_Logged(LoggerArgs args)
        {
            if (queue.Count > queueLimit && args.Priority != LogPriority.Debug)
            {
                if (!queueLimitExceeded)
                {
                    queueLimitExceeded = true;
                    Logger.Trace("Queue is full");
                }
                return;
            }

            if (args.Priority == LogPriority.Trace || args.Priority == LogPriority.Error || args.Priority == LogPriority.Debug)
            {
                queue.Enqueue(args.LogString);
            }
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
            if (!Inited)
                return;

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
                Logger.Log(LogPriority.Info, "Can't write log to sd: " + ex.Message);
            }
        }

        public static void BaseDispose()
        {
            Logger.Logged -= Logger_Logged;
        }

        public static void Dispose()
        {
            //#if DEBUG
            //            if (Debugger.IsAttached)
            //            {
            //                // you should manually step over this line till queue count be more that 5
            //                while (queue.Count < 5) ;
            //            }
            //#endif
            Logger.Logged -= Logger_Logged;
            bool waitResult = queue.WaitTillQueueBeEmpty();

            writer.Flush();
            if (FlushCallback != null)
            {
                FlushCallback();
            }

            writer.Dispose();
        }
    }
}

#endif