using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SPOT.IO
{
    public sealed class VolumeInfo
    {
        public bool IsFormatted { private set; get; }
        public string RootDirectory { private set; get; }

        public static VolumeInfo[] GetVolumes()
        {
            return new VolumeInfo[]
            {
                new VolumeInfo()
                {
                    IsFormatted = true,
                    //RootDirectory = "SDC"
                    RootDirectory = "USB"
                }
            };
        }

        public void FlushAll() { }
    }
}
