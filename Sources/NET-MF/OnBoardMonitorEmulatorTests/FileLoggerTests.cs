﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using imBMW.Enums;
using imBMW.iBus;
using imBMW.Tools;
using Microsoft.SPOT.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class FileLoggerTests : TestBaseWithLauncher
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }

        public sealed class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                return SafeNativeMethods.StrCmpLogicalW(a, b);
            }
        }

        [TestMethod]
        public void Should_StoreLoggedMessages_TillFileLoggerWillBeInited_IncludingDebugMessages()
        {
            FileLogger.Create();

            for (byte i = 0; i < FileLogger.queueLimit+20; i++)
            {
                Logger.Trace(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, i));
            }
            Logger.Debug("test1");
            Logger.Debug("test2");

            string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
            var logsPath = Path.Combine(rootDirectory, "logs");
            FileLogger.Init(logsPath, rootDirectory, () => VolumeInfo.GetVolumes()[0].FlushAll());

            FileLogger.Dispose(100000);

            var files = Directory.GetFiles(logsPath, "traceLog*").OrderBy(x => x, new NaturalStringComparer()).ToArray();
            var lastTraceLog = files[files.Length - 1];
            var logData = File.ReadLines(lastTraceLog).ToArray();
            Assert.AreEqual(logData.Length, FileLogger.queueLimit + 3);
            for (int i = 0; i < FileLogger.queueLimit; i++)
            {
                string testValue = "IKE > GLO: " + i.ToString("X2");
                Assert.IsTrue(logData[i].Contains(testValue));
            }
            Assert.IsTrue(logData[FileLogger.queueLimit].Contains("Queue is full"));
            Assert.IsTrue(logData[FileLogger.queueLimit + 1].Contains("test1"));
            Assert.IsTrue(logData[FileLogger.queueLimit + 2].Contains("test2"));

            ShouldDisposeManagersAndFileLogger = false;
        }

        [TestMethod]
        public void Should_StoreFatalErrors_InSeparateFile()
        {
            FileLogger.Create();
            string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
            var logsPath = Path.Combine(rootDirectory, "logs");
            FileLogger.Init(logsPath, rootDirectory, () => VolumeInfo.GetVolumes()[0].FlushAll());

            Logger.Trace("test trace");
            Logger.FatalError(ErrorIdentifier.SleepModeFlowBrokenErrorId);
            Logger.Debug("test debug");
            Logger.Warning("test warning");
            Logger.FatalError(ErrorIdentifier.UknownError);

            FileLogger.Dispose(10000);

            var logFiles = Directory.GetFiles(logsPath, "traceLog*").OrderBy(x => x, new NaturalStringComparer()).ToArray();
            var lastLog = logFiles[logFiles.Length - 1];
            var logData = File.ReadLines(lastLog).ToArray();
            Assert.AreEqual(logData.Length, 5);
            Assert.IsTrue(logData.Last().Contains("Unknown"));

            var errorsFiles = Directory.GetFiles(rootDirectory, "Errors*").OrderBy(x => x, new NaturalStringComparer()).ToArray();
            Assert.AreEqual(errorsFiles.Length, 1); // should be always 1 file
            var errorsFile = errorsFiles[0];
            var errorsData = File.ReadLines(errorsFile).ToArray();
            Assert.IsTrue(errorsData.Length >= 2);
            Assert.IsTrue(errorsData[errorsData.Length - 2].Contains("Sleep mode"));
            Assert.IsTrue(errorsData[errorsData.Length - 1].Contains("Unknown"));

            ShouldDisposeManagersAndFileLogger = false;
        }
    }
}
