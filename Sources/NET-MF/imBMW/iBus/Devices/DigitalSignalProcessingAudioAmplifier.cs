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
            Manager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.DigitalSignalProcessingAudioAmplifier, DeviceAddress.Diagnostic, ProcessDiagMessageFromDSP);
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.DigitalSignalProcessingAudioAmplifier, ProcessMessageFromDSP);
        }

        public static void ProcessDiagMessageFromDSP(Message message)
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

        public static void ProcessMessageFromDSP(Message m)
        {
            if (m.Data[0] == 0x35) // 2020.07.26 -> traceLog56.log #575
            {
                short f80 = (short)(0x10 - m.Data[4]);
                short f200 = (short)(0x30 - m.Data[5]);
                short f500 = (short)(0x50 - m.Data[6]);
                short f1K = (short)(0x70 - m.Data[7]);
                short f2K = (short)(0x90 - m.Data[8]);
                short f5K = (short)(0xB0 - m.Data[9]);
                short f12K = (short)(0xD0 - m.Data[10]);

                f200 = f200 < 0 ? f200 : (short)(0x10 - f200);
                f500 = f500 < 0 ? f500 : (short)(0x10 - f500);
                f1K =  f1K < 0 ? f1K : (short)(0x10 - f1K);
                f2K =  f2K < 0 ? f2K : (short)(0x10 - f2K);
                f5K = f5K < 0 ? f5K : (short)(0x10 - f5K);
                f12K = f12K < 0 ? f12K : (short)(0x10 - f12K);

                m.ReceiverDescription = "GT Car memory response. Frequencies: "
                                        + "80: " +     f80/*.ToString("+#;-#;0")*/
                                        + "; 200: " + f200/*.ToString("+#;-#;0")*/
                                        + "; 500: " + f500/*.ToString("+#;-#;0")*/
                                        + "; 1K: " +   f1K/*.ToString("+#;-#;0")*/
                                        + "; 2K: " +   f2K/*.ToString("+#;-#;0")*/
                                        + "; 5K: " +   f5K/*.ToString("+#;-#;0")*/
                                        + "; 12K: " + f12K/*.ToString("+#;-#;0")*/;
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
