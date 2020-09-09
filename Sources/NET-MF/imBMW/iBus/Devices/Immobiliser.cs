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
            KBusManager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.Immobiliser, m => ProcessEWSMessage(m, BusType.KBus));
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.Immobiliser, m => ProcessEWSMessage(m, BusType.IBus));

            MultiFunctionSteeringWheel.ButtonPressed += button =>
            {
                if (button == MFLButton.RT) // ModeRadio for sure
                {
                    // GND > IKE: 40 0D 00 01
                    var setCode0001Message = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, 0x40, 0x0D, 0x00, 0x01);
                    Manager.Instance.EnqueueMessage(setCode0001Message);
                }
            };
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        static void ProcessEWSMessage(Message m, BusType bus)
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
                    m.ReceiverDescription = "Key" + LastKeyInserted + " Immobilisation deactivated";
                }
            }
        }

        public static event KeyInsertedEventHandler KeyInserted;

        public static event KeyRemovedEventHandler KeyRemoved;
    }
}
