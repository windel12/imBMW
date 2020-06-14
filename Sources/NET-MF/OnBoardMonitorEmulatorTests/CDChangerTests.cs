using System;
using imBMW.iBus;
using imBMW.Devices.V2;
using imBMW.iBus.Devices.Real;
using OnBoardMonitorEmulator.DevicesEmulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using imBMW.Features.Multimedia.iBus;

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

            var radioOnWaitHandle = MessageReceivedWaitHandle(new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press radio on/off", Radio.DataRadioKnobPressed));
            var CDPlayWaitHandle = MessageReceivedWaitHandle(new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, CDChanger.DataPlay));
            var CDResponseWaitHandle = MessageReceivedWaitHandle((Launcher.Emulator as CDChanger).StatusPlaying(1, 1));
            Radio.PressOnOffToggle();

            bool result1 = radioOnWaitHandle.WaitOne();
            bool result2 = CDPlayWaitHandle.WaitOne();
            bool result3 = CDResponseWaitHandle.WaitOne();

            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
            Assert.IsTrue(result3);
            Assert.IsTrue(Launcher.Emulator.IsEnabled);
            Assert.IsTrue(Launcher.Emulator.Player.IsPlaying);
        }
    }
}
