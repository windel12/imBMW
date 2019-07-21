using Microsoft.SPOT.IO;

namespace GHI.Usb.Host
{
    public class MassStorage
    {
        public bool Mounted { get; set; } = true;

        public void Mount()
        {
            RemovableMedia.FireInserted();
        }

        public void Unmount()
        {
            Mounted = false;
        }
    }
}
