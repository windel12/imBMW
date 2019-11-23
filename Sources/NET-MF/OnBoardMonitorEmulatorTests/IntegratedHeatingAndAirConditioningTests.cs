using System;
using System.Threading;
using imBMW.Enums;
using imBMW.Devices.V2;
using imBMW.iBus.Devices.Real;
using OnBoardMonitorEmulator.DevicesEmulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnBoardMonitorEmulatorTests.Helpers;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class IntegratedHeatingAndAirConditioningTests
    {
        [TestMethod]
        public void ShouldStartAndStopAuxilaryHeater()
        {
            AuxilaryHeaterEmulator.Init(2000);
            Launcher.Launch(Launcher.LaunchMode.WPF);

            ManualResetEvent waitHandle = new ManualResetEvent(false);
            AuxilaryHeaterStatus status1 = AuxilaryHeaterStatus.Unknown;
            AuxilaryHeaterStatus status2 = AuxilaryHeaterStatus.Unknown;
            AuxilaryHeaterStatus status3 = AuxilaryHeaterStatus.Unknown;
            AuxilaryHeaterStatus status4 = AuxilaryHeaterStatus.Unknown;
            AuxilaryHeater.StatusChanged += (status) =>
            {
                if (status == AuxilaryHeaterStatus.Present)
                    status1 = AuxilaryHeaterStatus.Present;

                if (status == AuxilaryHeaterStatus.StartPending)
                    status2 = AuxilaryHeaterStatus.StartPending;

                if (status == AuxilaryHeaterStatus.Started)
                    status3 = AuxilaryHeaterStatus.Started;

                if (status == AuxilaryHeaterStatus.Working)
                    status4 = AuxilaryHeaterStatus.Working;

                if (status == AuxilaryHeaterStatus.Working)
                    waitHandle.Set();
            };

            IntegratedHeatingAndAirConditioning.StartAuxilaryHeater();
            bool result = waitHandle.Wait(5000);
            Assert.IsTrue(result);
            Assert.IsTrue(status1 == AuxilaryHeaterStatus.Present);
            Assert.IsTrue(status2 == AuxilaryHeaterStatus.StartPending);
            Assert.IsTrue(status3 == AuxilaryHeaterStatus.Started);
            Assert.IsTrue(status4 == AuxilaryHeaterStatus.Working);


            waitHandle.Reset();
            AuxilaryHeaterStatus status5 = AuxilaryHeaterStatus.Stopping;
            AuxilaryHeaterStatus status6 = AuxilaryHeaterStatus.Stopped;
            AuxilaryHeater.StatusChanged += (status) =>
            {
                if (status == AuxilaryHeaterStatus.Stopping)
                    status5 = AuxilaryHeaterStatus.Stopping;

                if (status == AuxilaryHeaterStatus.Stopped)
                    status6 = AuxilaryHeaterStatus.Stopped;

                if (status == AuxilaryHeaterStatus.Stopped)
                    waitHandle.Set();
            };

            IntegratedHeatingAndAirConditioning.StopAuxilaryHeater();
            result = waitHandle.Wait(3000);
            Assert.IsTrue(result);
            Assert.IsTrue(status5 == AuxilaryHeaterStatus.Stopping);
            Assert.IsTrue(status6 == AuxilaryHeaterStatus.Stopped);
        }

        [TestMethod]
        public void ShouldRestoreAuxilaryHeaterStatus_IfThereWasRestart_DuringAuxilaryHeaterWorking()
        {
            AuxilaryHeaterEmulator.Init(2000);
            Launcher.Launch(Launcher.LaunchMode.WPF);

            ManualResetEvent waitHandle = new ManualResetEvent(false);
            AuxilaryHeater.StatusChanged += (status) =>
            {
                if (status == AuxilaryHeaterStatus.Working)
                    waitHandle.Set();
            };

            IntegratedHeatingAndAirConditioning.StartAuxilaryHeater();
            bool result = waitHandle.Wait(5000);
            Assert.IsTrue(result);


            waitHandle.Reset();
            AuxilaryHeater.Status = AuxilaryHeaterStatus.Unknown;
            result = waitHandle.Wait(3000);
            Assert.IsTrue(result);
            Assert.IsTrue(AuxilaryHeater.Status == AuxilaryHeaterStatus.Working);
        }
    }
}
