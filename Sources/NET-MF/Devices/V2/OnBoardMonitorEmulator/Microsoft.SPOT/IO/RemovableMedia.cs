using System;

namespace Microsoft.SPOT.IO
{
    public static class RemovableMedia
    {
        public static event EjectEventHandler Eject;
        public static event InsertEventHandler Insert;

        public static void FireInserted()
        {
            Insert(new object(), new MediaEventArgs(new VolumeInfo(), DateTime.Now));
        }
    }
}
