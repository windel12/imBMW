using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    public static class DigitalSignalProcessingAudioAmplifier
    {
        static DigitalSignalProcessingAudioAmplifier()
        {
            Manager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.DigitalSignalProcessingAudioAmplifier, DeviceAddress.Diagnostic, ProcessMessageFromDSP);
        }

        public static void ProcessMessageFromDSP(Message message)
        {
            if (message.Data[0] == 0xA0)
            {
                Logger.Info("DIAG OKAY");
            }
            if (message.Data[0] == 0xA1 || message.Data[0] == 0xA2)
            {
                Logger.Error("DIAG BUSY");
            }
        }

        public static void Reset()
        {
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Diagnostic, DeviceAddress.DigitalSignalProcessingAudioAmplifier, "DSP2.PRG->JOBNAME:RESET", 0x1C, 0x00));
        }

        public static void SelfTest()
        {
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Diagnostic, DeviceAddress.DigitalSignalProcessingAudioAmplifier, "DSP2.PRG->JOBNAME:DSP_SELBSTTEST ", 0x30));
        }
    }
}
