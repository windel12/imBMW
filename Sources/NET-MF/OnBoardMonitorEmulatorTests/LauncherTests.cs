﻿using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using imBMW.Devices.V2;
using imBMW.Features.Menu;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using OnBoardMonitorEmulator.DevicesEmulation;
using OnBoardMonitorEmulatorTests.Helpers;
using System.Threading.Tasks;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class LauncherTests
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

        [TestMethod]
        public void Should_StartApp_AndAcquireBordComputerData()
        {
            
        }

        // TODO: check that ResetBoard called correctly
        [TestMethod]
        public void Should_TurnOffRadio_AndResetBoard_WhenMenuRadioOnHold()
        {
            RadioEmulator.Init();

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

        [TestMethod]
        public void Should_IdleAndWakeUp()
        {
            var initializationWaitHandle = InitializationWaitHandle();
            var appStateChangedWaitHandle = AppStateChangedWaitHandle(AppState.Idle);
            var phoneButtonHoldWaitHandle = new ManualResetEvent(false);

            var wakeUp = new Func<bool>(() =>
            {
                var message = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, MessageRegistry.DataPollResponse);
                var messageReceivedWaitHandle = MessageReceivedWaitHandle(message);
                Manager.Instance.EnqueueMessage(message);
                var messageReceivedWaitHandleResult = messageReceivedWaitHandle.Wait();
                return messageReceivedWaitHandleResult;
            });

            
            Launcher.sleepTimeout = Launcher.GetTimeoutInMilliseconds(59, 59);
            Launcher.Launch(Launcher.LaunchMode.WPF);
            InstrumentClusterElectronics.CurrentIgnitionState = IgnitionState.Off;

            initializationWaitHandle.Wait();

            // Assert
            // going to idle
            Launcher.idleTime = Launcher.idleTimeout + Launcher.GetTimeoutInMilliseconds(0, 59);
            bool waitResult = appStateChangedWaitHandle.Wait(Launcher.requestIgnitionStateTimerPeriod * 2);
            Assert.IsTrue(waitResult, "0");
            Assert.IsTrue(Launcher._massStorage != null, "1");
            Assert.IsTrue(Launcher._massStorage.Mounted == false, "2");

            var wakeUpWaitHandleResult = wakeUp();
            Assert.IsTrue(wakeUpWaitHandleResult == true, "3");
            Assert.IsTrue(Launcher._massStorage != null, "4");
            Assert.IsTrue(Launcher._massStorage.Mounted == true, "5");

            // Should not mount storage, if it was unmount manually
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, 0x48, 0x48));
            var phoneButtonHoldWaitHandleResult = phoneButtonHoldWaitHandle.Wait(2000);
            Assert.IsTrue(phoneButtonHoldWaitHandleResult, "6");
            Assert.IsTrue(Launcher._massStorage == null, "7");

            wakeUpWaitHandleResult = wakeUp();
            Assert.IsTrue(wakeUpWaitHandleResult == true, "8");
            Assert.IsTrue(Launcher._massStorage == null, "9");
        }

        [TestMethod]
        public void Should_GoToSleep_AndDisposeManagers()
        {
            var initializationWaitHandle = InitializationWaitHandle();
            var appStateChangedWaitHandle = AppStateChangedWaitHandle(AppState.Sleep);

            Launcher.idleTimeout = Launcher.GetTimeoutInMilliseconds(59, 59);
            Launcher.Launch(Launcher.LaunchMode.WPF);
            InstrumentClusterElectronics.CurrentIgnitionState = IgnitionState.Off;

            initializationWaitHandle.Wait();

            // Assert
            // going to sleep
            Launcher.idleTime = Launcher.sleepTimeout + Launcher.GetTimeoutInMilliseconds(0, 59);
            bool waitResult = appStateChangedWaitHandle.Wait(Launcher.requestIgnitionStateTimerPeriod * 2);
            Assert.IsTrue(waitResult, "0");
            Assert.IsTrue(Launcher._massStorage != null, "1");
            Assert.IsTrue(Launcher._massStorage.Mounted == false, "2");
        }


        private EventWaitHandle InitializationWaitHandle()
        {
            return MessageReceivedWaitHandle(new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, (byte) LedType.Green), 2);
        }

        private EventWaitHandle MessageReceivedWaitHandle(Message message, int messagesCount = 1)
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);

            int counter = 0;
            Manager.Instance.AddMessageReceiverForSourceAndDestinationDevice(message.SourceDevice, message.DestinationDevice, m =>
            {
                if (m.Data.Compare(message.Data))
                    counter++;
                if (counter == messagesCount)
                    waitHandle.Set();
            });

            return waitHandle;
        }

        private EventWaitHandle AppStateChangedWaitHandle(AppState state)
        {
            return ConditionWaitHandle(() => Launcher.State == state);
        }

        private EventWaitHandle ConditionWaitHandle(Func<bool> predicate)
        {
            var waitHandle = new ManualResetEvent(false);

            var checkAppStateThread = new Thread(() =>
            {
                while (!predicate())
                {
                    Thread.Sleep(100);
                }

                waitHandle.Set();
            });
            checkAppStateThread.Start();

            return waitHandle;
        }
    }
}
