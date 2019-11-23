using System;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public class KeyEventArgs
    {
        public byte KeyNumber { get; private set; }

        public KeyEventArgs(byte keyNumber)
        {
            KeyNumber = keyNumber;
        }
    }

    public delegate void KeyInsertedEventHandler(KeyEventArgs e);

    public delegate void KeyRemovedEventHandler(KeyEventArgs e);

    #endregion


    public static class Immobiliser
    {
        public static byte LastKeyInserted { get; private set; }
        public static bool IsKeyInserted { get; private set; }

        static Immobiliser()
        {
            KBusManager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.Immobiliser, ProcessEWSMessage);
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        static void ProcessEWSMessage(Message m)
        {
            if (m.Data.Length == 3 && m.Data[0] == 0x74)
            {
                if (m.Data[1] == 0x00) // No key in ignition switch
                {
                    if (IsKeyInserted)
                    {
                        var e = KeyRemoved;
                        if (e != null)
                        {
                            e(new KeyEventArgs(LastKeyInserted));
                        }
                        m.ReceiverDescription = "Key" + LastKeyInserted + " removed";
                        IsKeyInserted = false;
                        return;
                    }
                    m.ReceiverDescription = "No key inserted";
                }
                else if (m.Data[1] == 0x04) // Key in ignition switch
                {
                    if (!IsKeyInserted)
                    {
                        LastKeyInserted = m.Data[2];
                        var e = KeyInserted;
                        if (e != null)
                        {
                            e(new KeyEventArgs(LastKeyInserted));
                        }
                        m.ReceiverDescription = "Key" + LastKeyInserted + " inserted";
                        IsKeyInserted = true;
                        return;
                    }
                    m.ReceiverDescription = "Key" + LastKeyInserted + " into ignition switch";
                }
                else if (m.Data[1] == 0x05)
                {
                    m.ReceiverDescription = "Key" + LastKeyInserted + "immobilisation deactivated";
                }
            }
        }

        public static event KeyInsertedEventHandler KeyInserted;

        public static event KeyRemovedEventHandler KeyRemoved;
    }
}
