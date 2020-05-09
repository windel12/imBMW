using imBMW.Enums;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public enum MFLButton
    {
        Next,
        Prev,
        VolumeUp,
        VolumeDown,
        RT,
        ModeRadio,
        ModeTelephone,
        Dial,
        DialLong
    }

    public delegate void MFLEventHandler(MFLButton button);

    #endregion


    public class MultiFunctionSteeringWheel
    {
        static bool wasDialLongPressed;
        static bool needSkipRT;

        static Message MessagePhoneResponse = new Message(DeviceAddress.Telephone, DeviceAddress.LocalBroadcastAddress, 0x02, 0x00);

        /// <summary> 
        /// Emulate phone for right RT button commands
        /// </summary>
        public static bool EmulatePhone { get; set; }

        /// <summary>
        /// Use RT as button, not as radio/telephone modes toggle
        /// </summary>
        public static bool RTAsButton { get; set; } = true;

        static MultiFunctionSteeringWheel()
        {
            // TODO receive BM volume commands
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.MultiFunctionSteeringWheel, ProcessMFLMessage);
            InstrumentClusterElectronics.IgnitionStateChanged += InstrumentClusterElectronics_IgnitionStateChanged;
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        public static void VolumeUp(byte step = 1)
        {
            step = (byte)System.Math.Max((byte)1, System.Math.Min(step, (byte)9));
            var p = (byte)((step << 4) + 1);
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.MultiFunctionSteeringWheel, DeviceAddress.Radio, "Volume Up +" + step, 0x32, p));
        }

        public static void VolumeDown(byte step = 1)
        {
            step = (byte)System.Math.Max((byte)1, System.Math.Min(step, (byte)9));
            var p = (byte)(step << 4);
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.MultiFunctionSteeringWheel, DeviceAddress.Radio, "Volume Down -" + step, 0x32, p));
        }

        static void InstrumentClusterElectronics_IgnitionStateChanged(IgnitionEventArgs e)
        {
            if (e.CurrentIgnitionState != IgnitionState.Off && e.PreviousIgnitionState == IgnitionState.Off)
            {
                // MFL sends RT 00 signal on ignition OFF -> ACC
                needSkipRT = true;
            }
        }

        static void ProcessMFLMessage(Message m)
        {
            if (m.Data.Compare(MessageRegistry.DataPollRequest))
            {
                if (EmulatePhone)
                {
                    Manager.Instance.EnqueueMessage(MessagePhoneResponse);
                }
            }
            else if (m.Data.Length == 2 && m.Data[0] == 0x32)
            {
                switch (m.Data[1])
                {
                    case 0x10:
                    case 0x20:
                    case 0x30:
                    case 0x40:
                    case 0x50:
                    case 0x60:
                    case 0x70:
                    case 0x80:
                    case 0x90:
                        OnButtonPressed(m, MFLButton.VolumeDown);
                        break;
                    case 0x11:
                    case 0x21:
                    case 0x31:
                    case 0x41:
                    case 0x51:
                    case 0x61:
                    case 0x71:
                    case 0x81:
                    case 0x91:
                        OnButtonPressed(m, MFLButton.VolumeUp);
                        break;
                }
            }
            else if (m.Data.Length == 2 && m.Data[0] == 0x3B)
            {
                var btn = m.Data[1];
                switch (btn)
                {
                    case 0x01:
                        OnButtonPressed(m, MFLButton.Next);
                        break;
                    case 0x21:
                        OnButtonReleased(m, MFLButton.Next);
                        break;
                    case 0x08:
                        OnButtonPressed(m, MFLButton.Prev);
                        break;
                    case 0x28:
                        OnButtonReleased(m, MFLButton.Prev);
                        break;

                    case 0x40:
                    case 0x00:
                        if (RTAsButton)
                        {
                            if (!needSkipRT || btn == 0x40)
                            {
                                OnButtonPressed(m, MFLButton.RT);
                            }
                            else
                            {
                                m.ReceiverDescription = "RT (skipped)";
                            }
                        }
                        else
                        {
                            OnButtonPressed(m, btn == 0x00 ? MFLButton.ModeRadio : MFLButton.ModeTelephone);
                        }
                        needSkipRT = false;
                        break;

                    case 0x80:
                        wasDialLongPressed = false;
                        m.ReceiverDescription = "Dial pressed";
                        break;
                    case 0x90:
                        wasDialLongPressed = true;
                        OnButtonPressed(m, MFLButton.DialLong);
                        break;
                    case 0xA0:
                        if (!wasDialLongPressed)
                        {
                            OnButtonPressed(m, MFLButton.Dial);
                        }
                        else
                        {
                            m.ReceiverDescription = "Dial released";
                        }
                        wasDialLongPressed = false;
                        break;
                    default:
                        m.ReceiverDescription = "Button unknown " + btn.ToHex();
                        break;
                }
            }
        }

        static void OnButtonPressed(Message m, MFLButton button)
        {
            var e = ButtonPressed;
            if (e != null)
            {
                e(button);
            }
            m.ReceiverDescription = "MFL " + button.ToStringValue() + " pressed";
        }

        static void OnButtonReleased(Message m, MFLButton button)
        {
            m.ReceiverDescription = "MFL " + button.ToStringValue() + " released";
        }

        public static event MFLEventHandler ButtonPressed;
    }
}
