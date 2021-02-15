using System;
using imBMW.iBus;
using imBMW.Devices.V2;
using imBMW.iBus.Devices.Real;
using OnBoardMonitorEmulator.DevicesEmulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using imBMW.Features.Multimedia.iBus;
using OnBoardMonitorEmulatorTests.Helpers;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class CDChangerTests : TestBaseWithLauncher
    {
        [TestMethod]
        public void ShouldCorrectlyReply_WhenPlayingStarted()
        {
            RadioEmulator.Init();
            Launcher.Launch(Launcher.LaunchMode.WPF);

            var radioOnWaitHandle = MessageReceivedWaitHandle(new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press radio on/off", Radio.DataRadioKnobPressed), Manager.Instance);
            var CDPlayWaitHandle = MessageReceivedWaitHandle(new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, CDChanger.DataPlay), Manager.Instance);
            var CDResponseWaitHandle = MessageReceivedWaitHandle((Launcher.Emulator as CDChanger).StatusPlaying(1, 1), Manager.Instance);
            Radio.PressOnOffToggle();

            bool result1 = radioOnWaitHandle.Wait(5000);
            bool result2 = CDPlayWaitHandle.Wait(5000);
            bool result3 = CDResponseWaitHandle.Wait(5000);

            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
            Assert.IsTrue(result3);
            Assert.IsTrue(Launcher.Emulator.IsEnabled);
            Assert.IsTrue(Launcher.Emulator.Player.IsPlaying);
        }
    }
}
