using System;
using Microsoft.SPOT;

namespace imBMW.Enums.Volumio
{
    public enum VolumioCommands
    {
        Common = 0x00,
        Playback = 0x01,
        System = 0x02,
        ClearQueue = 0x03,
        AddPlaylistToQueue = 0x04,
    }
}
