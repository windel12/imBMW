using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using imBMW.Devices.V2;
using imBMW.iBus.Devices.Real;
using OnBoardMonitorEmulator.DevicesEmulation;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class LauncherTests
    {
        [TestMethod]
        public void Should_StartApp_AndAcquireDateTime()
        {
            ManualResetEvent waitHandler = new ManualResetEvent(false);

            InstrumentClusterElectronicsEmulator.Init();

            InstrumentClusterElectronics.DateTimeChanged += (e) => { waitHandler.Set(); };

            var now = DateTime.Now;
            Launcher.Launch(Launcher.LaunchMode.WPF);

            waitHandler.WaitOne(Debugger.IsAttached ? 30000 : 1000);

            Assert.AreEqual(Utility.CurrentDateTime.Year, now.Year);
            Assert.AreEqual(Utility.CurrentDateTime.Month, now.Month);
            Assert.AreEqual(Utility.CurrentDateTime.Day, now.Day);
            Assert.AreEqual(Utility.CurrentDateTime.Hour, now.Hour);
            Assert.AreEqual(Utility.CurrentDateTime.Minute, now.Minute);
        }

        [TestMethod]
        public void Should_StartApp_AndAcquireBordComputerData()
        {
            
        }
    }
}
