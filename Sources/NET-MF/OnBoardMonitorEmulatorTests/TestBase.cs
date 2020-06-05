using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imBMW.Devices.V2;
using imBMW.Features;
using imBMW.iBus;
using imBMW.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class TestBase
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
