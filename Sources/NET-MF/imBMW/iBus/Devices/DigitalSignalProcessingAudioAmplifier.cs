using System;
using Microsoft.SPOT;
using imBMW.Tools;
using imBMW.Enums;

namespace imBMW.iBus.Devices.Real
{
    public static class DigitalSignalProcessingAudioAmplifier
    {
        private static bool IsAnnounced { get; set; }

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

            if (message.Data.StartsWith(MessageRegistry.DataAnnounce))
            {
                if (!IsAnnounced)
                {
                    IsAnnounced = true;
                }
                else
                {
                    Logger.Warning("DSP WAS RESETTED!");
                }
            }
        }

        public static void ChangeSource(AudioSource source)
        {
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.DigitalSignalProcessingAudioAmplifier/*LocalBroadcastAddress*/, 0x36, (byte)source));
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
