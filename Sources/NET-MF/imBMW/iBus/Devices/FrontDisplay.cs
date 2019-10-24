using System;
using imBMW.Tools;
using Microsoft.SPOT;

namespace imBMW.iBus.Devices.Real
{
    [Flags]
    public enum LedType : byte
    {
        Red = 1,
        RedBlinking = 2,
        Orange = 4,
        OrangeBlinking = 8,
        Green = 16,
        GreenBlinking = 32,
        Empty = 64
    }

    public static class FrontDisplay
    {
        private static LedType _currentLEDState;

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
                _currentLEDState = ledType | _currentLEDState;
            }
            else if (remove)
            {
                _currentLEDState = _currentLEDState & ~ledType;
            }
            else
            {
                _currentLEDState = ledType;
            }

            //if (blinkerOn)
            //{
            //    //b = b.AddBit(2);
            //}
            //if (player != null/* && player.IsPlaying*/)
            //{
            //    b = b.AddBit(4);
            //}
            var message = new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, (byte)_currentLEDState);
            Manager.Instance.EnqueueMessage(message);
        }
    }
}
