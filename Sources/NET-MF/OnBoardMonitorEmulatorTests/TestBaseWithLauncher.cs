using System;
using imBMW.Devices.V2;
using imBMW.Features;
using imBMW.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class TestBaseWithLauncher : TestBase
    {
        protected bool ShouldDisposeManagersAndFileLogger = true;

        [TestCleanup]
        public void TestCleanup()
        {
            if (ShouldDisposeManagersAndFileLogger)
            {
                Launcher.DisposeManagers();
                FileLogger.Dispose();
            }

            Launcher.LedBlinkingQueueThreadWorker.Dispose();
            Comfort.Dispose();           
        }
    }
}
