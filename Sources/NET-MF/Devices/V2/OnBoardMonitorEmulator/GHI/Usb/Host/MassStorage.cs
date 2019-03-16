using Microsoft.SPOT.IO;

namespace GHI.Usb.Host
{
    public class MassStorage
    {
        public void Mount()
        {
            RemovableMedia.FireInserted();
        }
    }
}
