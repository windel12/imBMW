using System;
using Microsoft.SPOT.IO;

namespace Microsoft.SPOT.IO
{
    public class MediaEventArgs
    {
        public readonly DateTime Time;
        public readonly VolumeInfo Volume;

        public MediaEventArgs(VolumeInfo volume, DateTime time)
        {
        }
    }
}
