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
                if (m.Data[1] == 0x04)
                {
                    if (!IsKeyInserted)
                    {
                        LastKeyInserted = m.Data[2];
                        var e = KeyInserted;
                        if (e != null)
                        {
                            e(new KeyEventArgs(LastKeyInserted));
                        }
                        Logger.Info(m.ReceiverDescription);
                    }
                    IsKeyInserted = true;
                    m.ReceiverDescription = "Key " + LastKeyInserted + " inserted";
                }
                //if (m.Data[1] == 0x05)
                //{
                //    //"Immobilisation_deactivated Valid_key_detected Key#" + m.Data[2];
                //}
                else if (m.Data[1] == 0x00)
                {
                    if (IsKeyInserted)
                    {
                        var e = KeyRemoved;
                        if (e != null)
                        {
                            e(new KeyEventArgs(LastKeyInserted));
                        }
                        Logger.Info("Key " + LastKeyInserted + " removed");
                    }
                    IsKeyInserted = false;
                    m.ReceiverDescription = "No key inserted";
                }
            }
        }

        public static event KeyInsertedEventHandler KeyInserted;

        public static event KeyRemovedEventHandler KeyRemoved;
    }
}
