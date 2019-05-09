using imBMW.iBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imBMW.iBus.Devices.Emulators;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class RadioEmulator
    {
        public static bool IsEnabled { get; set; }

        static RadioEmulator()
        {
            Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);

            // emulate radio response to CD changer
        }

        public static void Init() { }

        static void ProcessToRadioMessage(Message m)
        {
            if (m.Data.Compare(MessageRegistry.DataPollRequest))
            {
                var pollResponseMessage = new Message(DeviceAddress.Radio, DeviceAddress.GlobalBroadcastAddress, MessageRegistry.DataPollResponse);
                Manager.Instance.EnqueueMessage(pollResponseMessage);
            }
            if (m.Data.Length == 4 && m.Data.StartsWith(Bordmonitor.DataItemClicked) && m.Data[3] <= 9)
            {
                var index = m.Data[3];
                byte diskNumber = 1;
                if (index <= 2)
                {
                    diskNumber = (byte) (index + 1);
                }
                else if (index >= 5 && index <= 7)
                {
                    diskNumber = (byte) (index - 1);
                }
                else
                {
                    return;
                }
                var showTitleMessage = Bordmonitor.ShowText("CD " + diskNumber + "-", BordmonitorFields.Title, send: false);
                var selectDiskMessage = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, CDChanger.GetDataSelectDisk(diskNumber));
                Manager.Instance.EnqueueMessage(showTitleMessage, selectDiskMessage);
            }
            if (m.Data.Length == 8 && m.Data.StartsWith(0x39))
            {
                var diskNumber = m.Data[6];
                var trackNumber = m.Data[7];
                Bordmonitor.ShowText("CD " + (diskNumber) + "-" + (trackNumber), BordmonitorFields.Title, send: true);
            }
            if (m.Data.Length == 2 && m.Data.StartsWith(Radio.DataRadioKnobPressed))
            {
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.Broadcast, IsEnabled ? Radio.DataRadioOff : Radio.DataRadioOn));
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, IsEnabled ? CDChanger.DataStop : CDChanger.DataPlay));
            }
            if (m.Data.Length == 2 && m.Data.StartsWith(Radio.DataNextPressed))
            {
                var message = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, CDChanger.DataNext);
                Manager.Instance.EnqueueMessage(message);
            }
            if (m.Data.Length == 2 && m.Data.StartsWith(Radio.DataPrevPressed))
            {
                var message = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, CDChanger.DataPrev);
                Manager.Instance.EnqueueMessage(message);
            }
        }
    }
}
