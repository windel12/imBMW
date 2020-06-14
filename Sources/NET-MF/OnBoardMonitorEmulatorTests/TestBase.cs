using System;
using System.Threading;
using imBMW.Devices.V2;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace OnBoardMonitorEmulatorTests
{
    public class TestBase
    {
        protected EventWaitHandle InitializationWaitHandle()
        {
            return MessageReceivedWaitHandle(new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, (byte)LedType.Green), 1);
        }

        protected EventWaitHandle MessageReceivedWaitHandle(Message message, int messagesCount = 1)
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

        protected EventWaitHandle AppStateChangedWaitHandle(AppState state)
        {
            return ConditionWaitHandle(() => Launcher.State == state);
        }

        protected EventWaitHandle ConditionWaitHandle(Func<bool> predicate)
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
