using System;
using imBMW.iBus;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class HeadlightVerticalAimControlEmulator
    {
        public static void Init() { }

        static HeadlightVerticalAimControlEmulator()
        {
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Diagnostic, DeviceAddress.HeadlightVerticalAimControl, ProcessMessageToHeadlightVerticalAimControl);
        }

        static void ProcessMessageToHeadlightVerticalAimControl(Message m)
        {
            if (m.Data[0] == 0x0C) // 0x0C - get diag data
            {
                var statusSensorLessenResponseMessage = new Message(DeviceAddress.HeadlightVerticalAimControl, DeviceAddress.Diagnostic, 0xA0, 0x34, 0xB8);
                KBusManager.Instance.EnqueueMessage(statusSensorLessenResponseMessage);
                var test = statusSensorLessenResponseMessage.ToDS2MessageResponse();
            }
            if (m.Data[0] == 0x1C || m.Data[0] == 0x9F) // 0x0C - get diag data
            {
                var diagOK = new Message(DeviceAddress.HeadlightVerticalAimControl, DeviceAddress.Diagnostic, 0xA0);
                KBusManager.Instance.EnqueueMessage(diagOK);
                var test = diagOK.ToDS2MessageResponse();
            }
        }
    }
}
