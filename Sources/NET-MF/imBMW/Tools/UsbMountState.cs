using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public enum UsbMountState
    {
        NotInitialized,
        DeviceConnectFailed,
        UnknownDeviceConnected,
        MassStorageConnected,
        Mounted,
        Unmounted
    }
}
