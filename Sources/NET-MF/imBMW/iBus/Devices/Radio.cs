using System;
using imBMW.Tools;
using System.Threading;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegates, etc

    public delegate void RadioOnOffHandler(bool turnedOn);

    #endregion

    public static class Radio
    {
        public static byte[] DataRadioOn = new byte[] { 0x4A, 0xFF };
        public static byte[] DataRadioOff = new byte[] { 0x4A, 0x00 };

        public static byte[] DataRadioKnobPressed = new byte[] { 0x48, 0x06 };
        public static byte[] DataRadioKnobHold = new byte[] { 0x48, 0x46 };
        public static byte[] DataRadioKnobReleased = new byte[] { 0x48, 0x86 };

        public static byte[] DataNextPressed = new byte[] { 0x48, 0x00 };
        public static byte[] DataNextReleased = new byte[] { 0x48, 0x80 };

        public static byte[] DataPrevPressed = new byte[] { 0x48, 0x10 };
        public static byte[] DataPrevReleased = new byte[] { 0x48, 0x90 };

        //public static byte[] DataTonePressed = new byte[] { 0x48, 0x04 };
        //public static byte[] DataToneReleased = new byte[] { 0x48, 0x84 };

        //public static byte[] DataSelectPressed = new byte[] { 0x48, 0x20 };
        //public static byte[] DataSelectReleased = new byte[] { 0x48, 0xA0 };

        //public static byte[] DataFMPressed = new byte[] { 0x48, 0x31 };
        //public static byte[] DataFMReleased = new byte[] { 0x48, 0xB1 };

        //public static byte[] DataAMPressed = new byte[] { 0x48, 0x21 };
        //public static byte[] DataAMReleased = new byte[] { 0x48, 0xA1 };

        public static byte[] DataModePressed = new byte[] { 0x48, 0x23 };
        public static byte[] DataModeReleased = new byte[] { 0x48, 0xA3 };

        public static byte[] DataSwitchPressed = new byte[] { 0x48, 0x14 };
        public static byte[] DataSwitchReleased = new byte[] { 0x48, 0x94 };

        //public static byte[] DataNaviKnobPressed = new byte[] { 0x48, 0x05 };
        //public static byte[] DataNaviKnobHold = new byte[] { 0x48, 0x45 };
        //public static byte[] DataNaviKnobReleased = new byte[] { 0x48, 0x85 };


        public const byte DisplayTextOnMIDMaxLen = 11;
        public const byte DisplayTextOnIKEMaxLen = 18; // maybe 20?

        const int displayTextDelay = 200;

        static Timer displayTextDelayTimer;

        public static bool HasMID { get; set; }

        /// <summary>
        /// Fires on radio on/off. Only for BM54/24.
        /// </summary>
        public static event RadioOnOffHandler OnOffChanged;

        static Radio()
        {
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        static void ProcessRadioMessage(Message m)
        {
            var radioOnOffChanged = OnOffChanged;
            if (radioOnOffChanged != null)
            {
                if (m.Data.Compare(DataRadioOn))
                {
                    radioOnOffChanged(true);
                    m.ReceiverDescription = "Radio On";
                    return;
                }
                if (m.Data.Compare(DataRadioOff))
                {
                    radioOnOffChanged(false);
                    m.ReceiverDescription = "Radio Off";
                    return;
                }
            }
        }

        static void ClearTimer()
        {
            if (displayTextDelayTimer != null)
            {
                displayTextDelayTimer.Dispose();
                displayTextDelayTimer = null;
            }
        }

        public static void DisplayTextWithDelay(string s, TextAlign align = TextAlign.Left, Message[] messageSendAfter = null)
        {
            DisplayTextWithDelay(s, displayTextDelay, align, messageSendAfter);
        }

        public static void DisplayTextWithDelay(string s, ushort delay, TextAlign align = TextAlign.Left, Message[] messageSendAfter = null)
        {
            ClearTimer();

            displayTextDelayTimer = new Timer(delegate
            {
                DisplayText(s, align);
                if (messageSendAfter != null)
                {
                    Manager.Instance.EnqueueMessage(messageSendAfter);
                }
            }, null, delay, 0);
        }

        public static void DisplayText(string s, TextAlign align = TextAlign.Left)
        {
            ClearTimer();

            if (HasMID)
            {
                DisplayTextMID(s, align);
            }
            else
            {
                DisplayTextRadio(s, align);
            }
        }

        private static void DisplayTextMID(string s, TextAlign align)
        {
            byte[] data = new byte[] { 0x23, 0x40, 0x20 };
            data = data.PadRight(0x20, DisplayTextOnMIDMaxLen);
            data.PasteASCII(s.Translit(), 3, DisplayTextOnMIDMaxLen, align);
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.MultiInfoDisplay, "Show text \"" + s + "\" on MID", data));
        }

        private static void DisplayTextRadio(string s, TextAlign align)
        {
            byte[] data = new byte[] { 0x23, 0x42, 0x30 }; // can 0x23 be changed to 0x1A ???   and 0x42 on 0x62 ???
            data = data.PadRight(0xFF, DisplayTextOnIKEMaxLen);
            data.PasteASCII(s.Translit(), 3, DisplayTextOnIKEMaxLen, align);
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.InstrumentClusterElectronics, "Show text \"" + s + "\" on IKE", data));
        }

        /// <summary>
        /// Turns radio on/off. Only for BM54/24.
        /// </summary>
        public static void PressOnOffToggle()
        {
            Manager.Instance.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press radio on/off", DataRadioKnobPressed),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release radio on/off", DataRadioKnobReleased)
            );
        }

        /// <summary>
        /// Press Next. Only for BM54/24.
        /// </summary>
        public static void PressNext()
        {
            Manager.Instance.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press Next", DataNextPressed),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release Next", DataNextReleased)
           );
        }

        /// <summary>
        /// Press Prev. Only for BM54/24.
        /// </summary>
        public static void PressPrev()
        {
            Manager.Instance.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press Prev", DataPrevPressed),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release Prev", DataPrevReleased)
             );
        }

        /*
        /// <summary>
        /// Press Mode. Only for BM54/24.
        /// </summary>
        public static void PressMode()
        {
            Manager.Instance.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press Mode", DataModePressed),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release Mode", DataModeReleased)
            );
        }

        /// <summary>
        /// Press Switch Sides. Only for BM54/24.
        /// </summary>
        public static void PressSwitchSide()
        {
            Manager.Instance.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press Switch Sides", DataSwitchPressed),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release Switch Sides", DataSwitchReleased)
            );
        }

        /// <summary>
        /// Press FM. Only for BM54/24.
        /// </summary>
        public static void PressFM()
        {
            Manager.Instance.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press FM", DataFMPressed),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release FM", DataFMReleased)
            );
        }

        /// <summary>
        /// Press AM. Only for BM54/24.
        /// </summary>
        public static void PressAM()
        {
            Manager.Instance.EnqueueMessage(
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Press AM", DataAMPressed),
                new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, "Release AM", DataAMReleased)
            );
        }
        */
    }
}
