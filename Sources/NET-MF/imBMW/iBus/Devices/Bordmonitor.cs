using System;
using imBMW.Tools;
using System.Text;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, EventArgs, delegates

    public enum BordmonitorFields
    {
        /// <summary> T0 field, 11 chars </summary>
        Title,
        /// <summary> Bottom right corner - 4 symbols field.  </summary>
        T1,
        /// <summary> Top right corner - 3 symbols field. </summary>
        T2,
        /// <summary> Bottom left corner - 5 symbols field. </summary>
        T3,
        /// <summary> Top left corner - 3 symbols field. </summary>
        T4,
        /// <summary> Top center - 5 symbols field. </summary>
        T5,
        /// <summary> Bottom center - 11 symbols fields </summary>
        Status,
        /// <summary> One of 10 items, 23 chars </summary>
        Item,
    }

#if OnBoardMonitorEmulator
    public class BordmonitorText
    {
        bool parsed;

        byte index;

        string text;

        bool isChecked;

        BordmonitorText[] items;

        public BordmonitorFields Field { get; protected set; }

        public byte[] Data { get; protected set; }

        public BordmonitorText(BordmonitorFields field, byte[] data)
        {
            Field = field;
            Data = data;
        }

        public BordmonitorText(BordmonitorFields field, string text, byte index = 0, bool isChecked = false)
        {
            Field = field;
            Text = text;
            Index = index;
            IsChecked = isChecked;
            parsed = true;
        }

        public byte Index
        {
            get
            {
                Parse();
                return index;
            }
            protected set { index = value; }
        }

        public string Text
        {
            get
            {
                Parse();
                return text + (IsChecked ? " [x]" : ""); // TODO remove
            }
            protected set { text = value; }
        }

        public bool IsChecked
        {
            get
            {
                Parse();
                return isChecked;
            }
            protected set { isChecked = value; }
        }

        void Parse()
        {
            if (parsed)
            {
                return;
            }

            switch (Field)
            {
                case BordmonitorFields.Title:
                    Text = ASCIIEncoding.GetString(Data, Bordmonitor.DataShowTitle.Length, -1, false).Trim().ASCIIToUTF8();
                    break;
                case BordmonitorFields.T1:
                case BordmonitorFields.T2:
                case BordmonitorFields.T3:
                case BordmonitorFields.T4:
                case BordmonitorFields.T5:
                case BordmonitorFields.Status:
                    Text = ASCIIEncoding.GetString(Data, Bordmonitor.DataShowStatus.Length, -1, false).Trim().ASCIIToUTF8();
                    break;
                case BordmonitorFields.Item:
                    throw new Exception("Use ParseItems() instead.");
            }

            parsed = true;
        }

        public BordmonitorText[] ParseItems()
        {
            if (items != null)
            {
                return items;
            }

            if (Field != BordmonitorFields.Item)
            {
                throw new Exception("Wrong Field type.");
            }

            #if NETMF
            var res = new System.Collections.ArrayList();
            #else
            var res = new System.Collections.Generic.List<BordmonitorText>();
            #endif

            if (Data.Length > 3)
            {
                var index = (byte)(Data[3] & 0x0F);
                bool isChecked = false;
                byte offset = 4;
                for (byte i = offset; i < Data.Length; i++)
                {
                    var isNext = Data[i] == 0x06;
                    var isLast = i == Data.Length - 1;
                    if (isNext || isLast)
                    {
                        if (!isNext)
                        {
                            isChecked = Data[i] == 0x2A;
                        }
                        var s = ASCIIEncoding.GetString(Data, offset, i - offset + (isNext ? 0 : 1) - (isChecked ? 1 : 0), false).Trim().ASCIIToUTF8();
                        res.Add(new BordmonitorText(Field, s, index, isChecked));
                        index++;
                        if (isNext && isLast)
                        {
                            res.Add(new BordmonitorText(Field, "", index, false));
                        }
                        offset = (byte)(i + 1);
                        continue;
                    }
                    isChecked = Data[i] == 0x2A;
                }
            }

            #if NETMF
            items = (BordmonitorText[])res.ToArray(typeof(BordmonitorText));
            #else
            items = res.ToArray();
            #endif
            return items;
        }
    }
#endif
    #endregion

    public static class Bordmonitor
    {
        public static Message MessageRefreshScreen = new Message(DeviceAddress.Radio, DeviceAddress.GraphicsNavigationDriver, "Refresh screen", 0xA5, 0x60, 0x01, 0x00);
        //public static Message MessageDisableRadioMenu = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.Radio, "Disable radio menu", 0x45, 0x02); // Thanks to RichardP (Intravee) for these two messages
        //public static Message MessageEnableRadioMenu = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.Radio, "Enable radio menu", 0x45, 0x00);

        public static byte[] DataShowTitle =    new byte[] { 0x23, 0x62, 0x10 }; // <68 Length 3B> 23 62 10 <Text in ASCII Hex> <XOR>
        public static byte[] DataShowHeader =   new byte[] { 0xA5, 0x62, 0x01 };
        public static byte[] DataShowStatus =   new byte[] { 0xA5, 0x62, 0x01, /*In*/0x06/*dex*/ }; // <68 Length 3B> A5 62 01 <Index of the text field> <Text in ASCII Hex> <XOR>
        public static byte[] DataShowT1 =       new byte[] { 0xA5, 0x62, 0x01, /*In*/0x01/*dex*/ };
        public static byte[] DataShowT2 =       new byte[] { 0xA5, 0x62, 0x01, /*In*/0x02/*dex*/ };
        public static byte[] DataShowT3 =       new byte[] { 0xA5, 0x62, 0x01, /*In*/0x03/*dex*/ };
        public static byte[] DataShowT4 =       new byte[] { 0xA5, 0x62, 0x01, /*In*/0x04/*dex*/ };
        public static byte[] DataShowT5 =       new byte[] { 0xA5, 0x62, 0x01, /*In*/0x05/*dex*/ };

        public static byte[] DataDrawIndexMk2 = { 0xA5, 0x62, 0x00 };
        /// <summary> 0x21, 0x60, 0x00 </summary>
        public static byte[] DataDrawIndexMk34 = { 0x21, 0x60, 0x00 };

        //public static byte[] DataAUX = new byte[] { 0x23, 0x62, 0x10, 0x41, 0x55, 0x58, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

        /// <summary>0x31, 0x60, 0x00</summary>
        public static byte[] DataItemClicked = {0x31, 0x60, 0x00};

        public static NaviVersion NaviVersion { get; set; }

        /// <summary> Use translit for non-latin characters </summary>
        public static bool Translit { get; set; }

#if OnBoardMonitorEmulator
        /// <summary> Emulate response from navigation to radio for screen updates. </summary>
        public static bool ReplyToScreenUpdates { get; set; }
        public static byte[] DataUpdateScreen = new byte[] { 0xA5, 0x62, 0x01 };

        public static Message MessageClearScreen = new Message(DeviceAddress.Radio, DeviceAddress.GraphicsNavigationDriver, "Clear screen", 0x46, 0x0C);

        public delegate void BordmonitorTextHandler(BordmonitorText args);
        public static event BordmonitorTextHandler TextReceived;
        public static event Action ScreenCleared;
        public static event Action ScreenRefreshed;

        private static byte _screenUpdatedMenuCounter;
        private static byte _screenUpdatedCounter;
#endif

        static Bordmonitor()
        {
#if OnBoardMonitorEmulator
            Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.GraphicsNavigationDriver, ProcessNavGraphicsMessage);
#endif
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

#if OnBoardMonitorEmulator
        static void ProcessNavGraphicsMessage(Message m) // used for emulator
        {
            var ae = ScreenCleared; // This is the only usage? - no, for emulator
            if (ae != null && m.Data.Compare(MessageClearScreen.Data))
            {
                ae();
                //m.ReceiverDescription = "Clear screen";
                return;
            }

            ae = ScreenRefreshed; // This is the only usage? - no, for emulator
            if (ae != null && m.Data.Compare(MessageRefreshScreen.Data))
            {
                ae();
                //m.ReceiverDescription = "Refresh screen";
                OnScreenUpdated();
                return;
            }

            var e = TextReceived; // This is the only usage? - no, for emulator
            if (e != null || ReplyToScreenUpdates)
            {
                if (m.Data.StartsWith(DataDrawIndexMk2) || m.Data.StartsWith(DataDrawIndexMk34))
                {
                    var a = new BordmonitorText(BordmonitorFields.Item, m.Data);
                    if (e != null)
                    {
                        e(a);
                    }
                    //#if NETMF
                    //m.ReceiverDescription = "BM fill items";
                    //#else
                    //var s = "BM fill items";
                    //var items = a.ParseItems();
                    //foreach (var i in items)
                    //{
                    //    s += " | " + i.Index.ToHex() + "." + i.Text;
                    //}
                    //m.ReceiverDescription = s;
                    //#endif
                    OnScreenUpdated(true);
                }
                else if (m.Data.StartsWith(DataUpdateScreen))
                {
                    if (m.Data.StartsWith(DataShowHeader))
                    {
                        var headerIndex = m.Data[DataShowHeader.Length];
                        var field = EnumConverter.GetBordmonitorFieldFromIndex(headerIndex);
                        var a = new BordmonitorText(field, m.Data);
                        if (e != null)
                        {
                            e(a);
                        }
                        //#if NETMF
                        //m.ReceiverDescription = "BM show status";
                        //#else
                        //m.ReceiverDescription = "BM show status: " + a.Text;
                        //#endif
                    }
                    OnScreenUpdated(false);
                }
                else if (m.Data.StartsWith(DataShowTitle))
                {
                    var a = new BordmonitorText(BordmonitorFields.Title, m.Data);
                    if (e != null)
                    {
                        e(a);
                    }
                    //#if NETMF
                    //m.ReceiverDescription = "BM show title";
                    //#else
                    //m.ReceiverDescription = "BM show title: " + a.Text;
                    //#endif
                    OnScreenUpdated();
                }
            }
        }

        private static void OnScreenUpdated()
        {
            if (!ReplyToScreenUpdates)
            {
                return;
            }
            byte i = _screenUpdatedCounter;
            if (i > 0)
            {
                i--;
                _screenUpdatedCounter = 0;
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.Radio, "Screen updated messages: " + (i + 1), 0x22, 0x00, i));
            }

            i = _screenUpdatedMenuCounter;
            if (i > 0)
            {
                i--;
                _screenUpdatedMenuCounter = 0;
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.Radio, "Menu updated messages: " + (i + 1), 0x22, i, 0xFF));
            }
        }

        private static void OnScreenUpdated(bool isMenuUpdated)
        {
            if (isMenuUpdated)
            {
                _screenUpdatedMenuCounter++;
                if (_screenUpdatedMenuCounter >= 3)
                {
                    OnScreenUpdated();
                }
            }
            else
            {
                _screenUpdatedCounter++; 
                OnScreenUpdated();
            }
        }
#endif

        public static Message ShowText(string s, BordmonitorFields field, byte index = 0, bool isChecked = false, bool send = true)
        {
            return ShowText(s, TextAlign.Left, field, index, isChecked, send);
        }

        public static Message ShowText(string s, TextAlign align, BordmonitorFields field, byte index = 0, bool isChecked = false, bool send = true)
        {
            int len;
            byte[] data;
            
            if (Translit)
            {
                s = s.Translit();
            }
            switch (field)
            {
                case BordmonitorFields.Title:
                    len = 11;
                    data = DataShowTitle;
                    break;
                case BordmonitorFields.T1:
                    len = 4;
                    data = DataShowT1;
                    break;
                case BordmonitorFields.T2:
                    len = 3;
                    data = DataShowT2;
                    break;
                case BordmonitorFields.T3:
                    len = 5;
                    data = DataShowT3;
                    break;
                case BordmonitorFields.T4:
                    len = 3;
                    data = DataShowT4;
                    break;
                case BordmonitorFields.T5:
                    len = 5;
                    data = DataShowT5;
                    break;
                case BordmonitorFields.Status:
                    len = 11;
                    data = DataShowStatus;
                    break;
                case BordmonitorFields.Item:
                    if (isChecked)
                    {
                        len = 15;
                    }
                    else
                    {
                        switch (NaviVersion)
                        {
                            case Tools.NaviVersion.MK4:
                                len = System.Math.Min(23, s.Length);
                                break;
                            default:
                                len = 15;
                                break;
                        }
                    }
                    index += 0x40;
                    /*if (index == 0x47)
                    {
                        index = 0x7;
                    }*/

                    //Format of the message for index fields I-0 to I-5 ( MK2 navigation system)
                    //<68 Length 3B> A5 62 00 <Index of the text field> <Text in ASCII Hex> <XOR>
                    if (NaviVersion == Tools.NaviVersion.MK2)
                    {
                        data = DataDrawIndexMk2.Combine(index);
                    }
                    //Format of the message for index fields I-0 to I-5 ( MK3 and MK4 navigation system with split screen software)
                    //<68 Length 3B> 21 60 00 <Index of the text field> <Text in ASCII Hex> <XOR>
                    else
                    {
                        data = DataDrawIndexMk34.Combine(index);
                    }
                    break;
                default:
                    throw new Exception("TODO");
            }
            var offset = data.Length;
            data = data.PadRight(0x20, len);
            data.PasteASCII(Translit ? s : s.UTF8ToASCII(), offset, len);
            if (isChecked)
            {
                data[data.Length - 2] = 0x2A;
            }
            if (field == BordmonitorFields.Item && NaviVersion < Tools.NaviVersion.MK4)
            {
                data[data.Length - 1] = 0x06;
            }
            var m = new Message(iBus.DeviceAddress.Radio, iBus.DeviceAddress.GraphicsNavigationDriver, "Show message on BM (" + index.ToHex() + "): " + s, data);
            if (send)
            {
                Manager.Instance.EnqueueMessage(m);
            }
            return m;
        }

#if OnBoardMonitorEmulator
        public static void PressItem(byte index)
        {
            index &= 0x0F;
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.Radio, "Press Screen item #" + index, DataItemClicked, index));
            index += 0x40;
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.Radio, "Release Screen item #" + index, DataItemClicked, index));
        }
#endif

        //public static void RefreshScreen()
        //{
        //    Manager.Instance.EnqueueMessage(MessageRefreshScreen);
        //}

        //public static void DisableRadioMenu()
        //{
        //    Manager.Instance.EnqueueMessage(MessageDisableRadioMenu);
        //}

        //public static void EnableRadioMenu()
        //{
        //    Manager.Instance.EnqueueMessage(MessageEnableRadioMenu);
        //}

        public static byte GetItemIndex(int count, byte index, bool back = false)
        {
            if (index > 9)
            {
                index -= 0x40;
            }
            // TODO also try 1-3 & 6-8
            var smallscreenOffset = count > 6 ? 0 : 2;
            if (back)
            {
                if (index > 2 && index < smallscreenOffset + 3)
                {
                    index += (byte)(3 + smallscreenOffset);
                }
                smallscreenOffset *= -1;
            }
            return (byte)(index <= 2 ? index : index + smallscreenOffset);
        }

    }
}
