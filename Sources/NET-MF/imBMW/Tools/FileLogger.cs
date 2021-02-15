#if !MF_FRAMEWORK_VERSION_V4_1

using System;
using System.Collections;
using System.IO;
using System.Threading;
using imBMW.iBus.Devices.Real;
using Debug = Microsoft.SPOT.Debug;

namespace imBMW.Tools
{
    public class FileLogger
    {
        const byte flushLines = 5;

        internal static byte queueLimit = 150;

        static ushort unflushed = 0;

        static StreamWriter writer;
        //static StreamWriter errorsWriter;
        static QueueThreadWorker queue;
        static Action FlushCallback;

        public static string FullPath { get; set; }
        private static string _errorsPath;

        public static string ERROR_FILE_NAME = "ERRORS.log";

        private static bool queueLimitExceeded;

        public static void Create()
        {
            if (queue == null)
            {
#if OnBoardMonitorEmulator
                queue = new QueueThreadWorker(ProcessItem, "fileLoggerThread", ThreadPriority.Normal, true);
#else
                queue = new QueueThreadWorker(ProcessItem, "fileLoggerThread", ThreadPriority.Lowest, true);
#endif
            }

            Logger.Logged += Logger_Logged;
        }

        public static void Dispose(int waitTimeout = 2000)
        {
            Logger.Logged -= Logger_Logged;
            bool waitResult = queue.WaitTillQueueBeEmpty(waitTimeout);
            queue.Dispose();

            writer.Flush();
            //errorsWriter.Flush();
            if (FlushCallback != null)
            {
                FlushCallback();
            }
            Debug.GC(true);
            unflushed = 0;

            writer.Dispose();
            //errorsWriter.Dispose();

            Thread.Sleep(1000);
        }

        public static void Init(string logsPath, string errorsPath, Action flushCallback = null)
        {
            try
            {
                FlushCallback = flushCallback;
                Logger.Debug("FlushCallback = flushCallback;");

                if (!Directory.Exists(logsPath))
                {
                    Directory.CreateDirectory(logsPath);
                }

                Logger.Debug("Going to get a list of files in 'logsPath'");
                string[] filesEnumerator = Directory.GetFiles(logsPath);
                
                //int filesCount = 0;
                //foreach (string file in filesEnumerator)
                //{
                //    filesCount++;
                //}

                Logger.Debug("Files in log folder count: " + filesEnumerator.Length);
                FullPath = logsPath + @"\traceLog" + filesEnumerator.Length + ".log";

                Logger.Debug("Log file StreamWriter creating");
                writer = new StreamWriter(FullPath, append:true);
                Logger.Debug("Log file StreamWriter created");

                _errorsPath = errorsPath;
                //Logger.Debug("Error file StreamWriter creating");
                //errorsWriter = new StreamWriter(errorsPath + @"\" + ERROR_FILE_NAME, append: true);
                //Logger.Debug("Error file StreamWriter creating");

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
            if (args.Priority == LogPriority.FatalError)
            {
                Logger.Debug("Error file StreamWriter creating");
                var errorsWriter = new StreamWriter(_errorsPath + @"\" + ERROR_FILE_NAME, append: true);
                Logger.Debug("Error file StreamWriter created");

                errorsWriter.WriteLine(args.LogString);
                errorsWriter.Flush();
                errorsWriter.Dispose();

                Logger.Debug("Error file StreamWriter disposed");
            }

            if (queue.Count < queueLimit || args.Priority == LogPriority.Debug || args.Priority == LogPriority.Error || args.Priority == LogPriority.FatalError)
            {
                queue.Enqueue(args.LogString);

                if (queue.Count < queueLimit - 10)
                    queueLimitExceeded = false;
            }
            if (queue.Count == queueLimit && !queueLimitExceeded)
            {
                queueLimitExceeded = true;
                Logger.Debug("Queue is full");
            }

            
#if (NETMF && !RELEASE) || OnBoardMonitorEmulator
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Debug.Print(args.LogString);
                Debug.Print("Free memory:" + Debug.GC(true));
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
                    Debug.GC(true);
                    unflushed = 0;
                }

                Thread.Sleep(0);
            }
            catch (Exception ex)
            {
                // don't use logger to prevent recursion
                Logger.ErrorWithoutLogging("Can't write log: " + ex.Message);
            }
        }

        public static void Eject()
        {
            Logger.Logged -= Logger_Logged;
            queue.Clear();
        }
    }
}

#endif