using System;
using imBMW.Tools;
using Microsoft.SPOT;

namespace imBMW.iBus.Devices.Real
{
    [Flags]
    public enum LedType : byte
    {
        Red = 0x01,
        RedBlinking = 0x02,
        Orange = 0x04,
        OrangeBlinking = 0x08,
        Green = 0x10,
        GreenBlinking = 0x20,
        Empty = 0x40
    }

    public static class FrontDisplay
    {
        public static LedType CurrentLEDState;

        /// <summary> 2A 00 00 </summary>
        public static readonly Message AuxHeaterIndicatorTurnOffMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x00);

        /// <summary> 2A 00 04 </summary>
        public static readonly Message AuxHeaterIndicatorTurnOnMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x04);

        /// <summary> 2A 00 08 </summary>
        public static readonly Message AuxHeaterIndicatorBlinkingMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x08);

        public static void RefreshLEDs(LedType ledType, bool append = false, bool remove = false)
        {
            if (!Manager.Instance.Inited)
            {
                return;
            }

            if (append)
            {
                CurrentLEDState = ledType | CurrentLEDState;
            }
            else if (remove)
            {
                CurrentLEDState = CurrentLEDState &~ ledType;
            }
            else
            {
                CurrentLEDState = ledType;
            }

            //if (blinkerOn)
            //{
            //    //b = b.AddBit(2);
            //}
            //if (player != null/* && player.IsPlaying*/)
            //{
            //    b = b.AddBit(4);
            //}
            var message = new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, (byte)CurrentLEDState);
            Manager.Instance.EnqueueMessage(message);
        }
    }
}
