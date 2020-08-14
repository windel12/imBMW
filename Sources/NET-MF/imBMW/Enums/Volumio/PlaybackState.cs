using System;
using Microsoft.SPOT;

namespace imBMW.Enums.Volumio
{
    public enum PlaybackState
    {
        Stop = 0x01,
        Pause = 0x02,
        Play = 0x03,
        Prev = 0x04,
        Next = 0x05,
        Seek = 0x06
    }
}
