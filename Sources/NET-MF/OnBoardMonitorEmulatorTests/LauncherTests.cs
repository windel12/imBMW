using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using imBMW.Devices.V2;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using OnBoardMonitorEmulator.DevicesEmulation;
using OnBoardMonitorEmulatorTests.Helpers;
using Microsoft.SPOT.IO;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class LauncherTests : TestBaseWithLauncher
    {
        [TestInitialize]
        public void Initialize()
        {
            
        }

        [TestMethod]
        public void Should_StartApp_AndAcquireDateTime()
        {
            ManualResetEvent dateTimeChangedWaitHandler = new ManualResetEvent(false);

            InstrumentClusterElectronicsEmulator.Init();

            InstrumentClusterElectronics.DateTimeChanged += (e) => { dateTimeChangedWaitHandler.Set(); };

            var now = DateTime.Now;
            Launcher.Launch(Launcher.LaunchMode.WPF);

            dateTimeChangedWaitHandler.WaitOne(Debugger.IsAttached ? 30000 : 1000);

            Assert.AreEqual(Utility.CurrentDateTime.Year, now.Year);
            Assert.AreEqual(Utility.CurrentDateTime.Month, now.Month);
            Assert.AreEqual(Utility.CurrentDateTime.Day, now.Day);
            Assert.AreEqual(Utility.CurrentDateTime.Hour, now.Hour);
            Assert.AreEqual(Utility.CurrentDateTime.Minute, now.Minute);
        }

        [Ignore]
        [TestMethod]
        public void Should_StartApp_AndAcquireBordComputerData()
        {
            
        }

        // TODO: check that ResetBoard called correctly
        [TestMethod]
        public void Should_TurnOffRadio_AndResetBoard_WhenMenuRadioOnHold()
        {
            RadioEmulator.Init();
            VolumioUartPlayerEmulator.Init();

            Launcher.Launch(Launcher.LaunchMode.WPF);

            ManualResetEvent emulatorOnIsEnabledChangedWaitHandle = new ManualResetEvent(false);
            Launcher.Emulator.IsEnabledChanged += (s, e) => { emulatorOnIsEnabledChangedWaitHandle.Set(); };
            Radio.PressOnOffToggle();
            bool radioEnabledResult = emulatorOnIsEnabledChangedWaitHandle.Wait();


            ManualResetEvent emulatorOnIsPlayingChangedWaitHandle = new ManualResetEvent(false);
            Launcher.Emulator.PlayerIsPlayingChanged += (sender, isPlayingChangedValue) =>
            {
                if (!isPlayingChangedValue)
                {
                    emulatorOnIsPlayingChangedWaitHandle.Set();
                }
            };
            Manager.Instance.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.LocalBroadcastAddress, 0x48, 0x34),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.LocalBroadcastAddress, 0x48, 0x74));
            bool radioDisabledResult = emulatorOnIsPlayingChangedWaitHandle.Wait();

            Assert.IsTrue(radioEnabledResult);
            Assert.IsTrue(radioDisabledResult);
            Assert.IsTrue(!Launcher.Emulator.IsEnabled);
        }

        [Ignore] // WakeUp after idle not implemented yet
        [TestMethod]
        public void Should_IdleAndUnmountStorage_AndThen_WakeUpAndMountStorageAgain()
        {
            var initializationWaitHandle = InitializationWaitHandle();
            var appStateChangedWaitHandle = AppStateChangedWaitHandle(AppState.Idle);
            var phoneButtonHoldWaitHandle = new ManualResetEvent(false);

            var wakeUpBySendingMessageToBus = new Func<bool>(() =>
            {
                var message = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, MessageRegistry.DataPollResponse);
                var messageReceivedWaitHandle = MessageReceivedWaitHandle(message);
                Manager.Instance.EnqueueMessage(message);
                var messageReceivedWaitHandleResult = messageReceivedWaitHandle.Wait();
                return messageReceivedWaitHandleResult;
            });

            var sleepTimeout = typeof(Launcher).GetField("sleepTimeout", BindingFlags.Static | BindingFlags.NonPublic);
            sleepTimeout.SetValue(null, Launcher.GetTimeoutInMilliseconds(59, 59));
            Launcher.Launch(Launcher.LaunchMode.WPF);
            InstrumentClusterElectronics.CurrentIgnitionState = IgnitionState.Off;

            bool initializationResult = initializationWaitHandle.Wait();
            Assert.IsTrue(initializationResult, "-1");

            // Assert
            // going to idle
            Launcher.idleTime = Launcher.idleTimeout + Launcher.GetTimeoutInMilliseconds(0, 59);
            bool waitResult = appStateChangedWaitHandle.Wait(Launcher.requestIgnitionStateTimerPeriod * 2);
            Assert.IsTrue(waitResult, "0");
            Assert.IsTrue(Launcher._massStorage != null, "1");
            Assert.IsTrue(Launcher._massStorage.Mounted == false, "2");

            var wakeUpWaitHandleResult = wakeUpBySendingMessageToBus();
            Assert.IsTrue(wakeUpWaitHandleResult == true, "3");
            Assert.IsTrue(Launcher._massStorage != null, "4");
            Assert.IsTrue(Launcher._massStorage.Mounted == true, "5");

            // Should not mount storage, if it was unmount manually
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, 0x48, 0x48));
            var phoneButtonHoldWaitHandleResult = phoneButtonHoldWaitHandle.Wait(2000);
            Assert.IsTrue(phoneButtonHoldWaitHandleResult, "6");
            Assert.IsTrue(Launcher._massStorage == null, "7");

            wakeUpWaitHandleResult = wakeUpBySendingMessageToBus();
            Assert.IsTrue(wakeUpWaitHandleResult == true, "8");
            Assert.IsTrue(Launcher._massStorage == null, "9");
        }

        [TestMethod]
        public void Should_GoToSleep_AndDisposeManagers()
        {
            var initializationWaitHandle = InitializationWaitHandle();
            var appStateChangedWaitHandle = AppStateChangedWaitHandle(AppState.Sleep);

            var idleTimeout = typeof(Launcher).GetField("idleTimeout", BindingFlags.Static | BindingFlags.NonPublic);
            idleTimeout.SetValue(null, Launcher.GetTimeoutInMilliseconds(59, 59));
            Launcher.Launch(Launcher.LaunchMode.WPF);
            InstrumentClusterElectronics.CurrentIgnitionState = IgnitionState.Off;

            bool initializationResult = initializationWaitHandle.Wait();
            Assert.IsTrue(initializationResult, "-1");

            // Assert
            // going to sleep
            Launcher.idleTime = Launcher.sleepTimeout + Launcher.GetTimeoutInMilliseconds(0, 59);
            bool waitResult = appStateChangedWaitHandle.Wait(Launcher.requestIgnitionStateTimerPeriod * 2);
            Assert.IsTrue(waitResult, "0");
            Assert.IsTrue(Launcher._massStorage != null, "1");
            Assert.IsTrue(Launcher._massStorage.Mounted == false, "2");

            ShouldDisposeManagersAndFileLogger = false;
        }

        [TestMethod]
        public void Should_StoreError_ByExceedingIdelOverallTimeout()
        {
            var errorFilePath = Path.Combine(VolumeInfo.GetVolumes()[0].RootDirectory, FileLogger.ERROR_FILE_NAME);
            var errorsFile = File.Create(errorFilePath);
            errorsFile.Dispose();

            var initializationWaitHandle = InitializationWaitHandle();
            var appStateChangedWaitHandle = AppStateChangedWaitHandle(AppState.Sleep);

            Launcher.Launch(Launcher.LaunchMode.WPF);
            InstrumentClusterElectronics.CurrentIgnitionState = IgnitionState.Off;

            bool initializationResult = initializationWaitHandle.Wait();
            Assert.IsTrue(initializationResult, "-1");

            // Assert
            // going to sleep by overall timeout
            Launcher.idleOverallTime = Launcher.idleOverallTimeout + Launcher.GetTimeoutInMilliseconds(0, 59);
            bool waitResult = appStateChangedWaitHandle.Wait(Launcher.requestIgnitionStateTimerPeriod * 2);
            Assert.IsTrue(waitResult, "0");
            Assert.IsTrue(Launcher._massStorage != null, "1");
            Assert.IsTrue(Launcher._massStorage.Mounted == false, "2");

            var errorsData = File.ReadLines(errorFilePath).ToArray();
            Assert.IsTrue(errorsData.Length >= 1);
            Assert.IsTrue(errorsData[errorsData.Length - 1].Contains("[FATAL] Sleep mode flow was broken"));

            ShouldDisposeManagersAndFileLogger = false;
        }

        [TestMethod]
        public void Should_ShowIKEMessage_IfErrorLogContainsData()
        {
            var errorsFile = File.Open(Path.Combine(VolumeInfo.GetVolumes()[0].RootDirectory, FileLogger.ERROR_FILE_NAME), FileMode.OpenOrCreate);
            errorsFile.WriteByte(ErrorIdentifier.SleepModeFlowBrokenErrorId);
            errorsFile.WriteByte(ErrorIdentifier.UknownError);
            errorsFile.Dispose();

            var text = "ERRORS FOUND. CHECK LOG!";
            var data = new byte[] { 0x1A, 0x37, (byte)TextMode.WithGong1 };
            data = data.PadRight(0x20, InstrumentClusterElectronics.DisplayTextOnIKEMaxLen);
            data.PasteASCII(text.Translit(), 3, InstrumentClusterElectronics.DisplayTextOnIKEMaxLen, TextAlign.Left);
            var waitHandle = MessageReceivedWaitHandle(new Message(DeviceAddress.CheckControlModule, DeviceAddress.InstrumentClusterElectronics, data));
            Task.Factory.StartNew(() =>
            {
                Launcher.Launch(Launcher.LaunchMode.WPF);
            });
            bool result = waitHandle.Wait(10000);
            Assert.IsTrue(result);

            ShouldDisposeManagersAndFileLogger = false;
        }
    }
}
