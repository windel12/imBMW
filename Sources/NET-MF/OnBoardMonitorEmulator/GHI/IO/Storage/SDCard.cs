using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SPOT.IO;

namespace GHI.IO.Storage
{
    public class SDCard
    {
        public SDCard()
        {
        }

        public SDCard(SDInterface sdInterface)
        {
        }

        public void Mount()
        {
            RemovableMedia.FireInserted();
        }

        public void Dispose() { }

        protected void Dispose(bool disposing) { }

        public enum SDInterface
        {
            //
            // Summary:
            //     The default MCI interface.
            MCI = 0,
            //
            // Summary:
            //     Access the SD card over SPI (device dependent).
            SPI = 1
        }
    }
}
