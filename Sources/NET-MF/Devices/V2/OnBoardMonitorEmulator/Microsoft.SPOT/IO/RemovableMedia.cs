using System;

namespace Microsoft.SPOT.IO
{
    public static class RemovableMedia
    {
        public static event EjectEventHandler Eject;
        public static event InsertEventHandler Insert;

        public static void FireInserted()
        {
            var volumeInfo = new VolumeInfo();
            var mediaEventArgs = new MediaEventArgs(volumeInfo, DateTime.Now);
            Insert(new object(), mediaEventArgs);
        }
    }
}
