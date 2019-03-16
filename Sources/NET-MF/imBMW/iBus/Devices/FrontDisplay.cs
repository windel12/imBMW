using System;
using Microsoft.SPOT;

namespace imBMW.iBus.Devices.Real
{
    public static class FrontDisplay
    {
        /// <summary> 2A 00 00 </summary>
        public static readonly Message AuxHeaterIndicatorTurnOffMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x00);

        /// <summary> 2A 00 04 </summary>
        public static readonly Message AuxHeaterIndicatorTurnOnMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x04);

        /// <summary> 2A 00 08 </summary>
        public static readonly Message AuxHeaterIndicatorBlinkingMessage = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x08);
    }
}
