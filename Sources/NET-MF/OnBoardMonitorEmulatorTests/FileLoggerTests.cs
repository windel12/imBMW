using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using imBMW.iBus;
using imBMW.Tools;
using Microsoft.SPOT.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class FileLoggerTests
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
            FileLogger.Init(rootDirectory, () => VolumeInfo.GetVolumes()[0].FlushAll());

            FileLogger.Dispose(100000);

            var files = Directory.GetFiles(rootDirectory, "traceLog*").OrderBy(x => x, new NaturalStringComparer()).ToArray();
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
        }
    }
}
