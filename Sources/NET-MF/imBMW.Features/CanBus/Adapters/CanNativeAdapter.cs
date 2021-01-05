using System;
using Microsoft.SPOT;
using GHI.IO;

namespace imBMW.Features.CanBus.Adapters
{
    public class CanNativeAdapter : CanAdapter
    {
        ControllerAreaNetwork can;

        public CanNativeAdapter(ControllerAreaNetwork.Channel canPort, CanAdapterSettings.CanSpeed speed)
            : this(new CanNativeAdapterSettings(canPort, speed))
        { }

        public CanNativeAdapter(ControllerAreaNetwork.Channel canPort, ControllerAreaNetwork.Timings timings)
            : this(new CanNativeAdapterSettings(canPort, 0), timings)
        { }

        protected CanNativeAdapter(CanNativeAdapterSettings settings, ControllerAreaNetwork.Timings timings = null)
            : base(settings)
        {
            switch (settings.Speed)
            {
                case CanAdapterSettings.CanSpeed.Kbps1000:
                    can = new ControllerAreaNetwork(settings.CanPort, ControllerAreaNetwork.Speed.Kbps1000);
                    break;
                case CanAdapterSettings.CanSpeed.Kbps500:
                    can = new ControllerAreaNetwork(settings.CanPort, ControllerAreaNetwork.Speed.Kbps500);
                    break;
                case CanAdapterSettings.CanSpeed.Kbps250:
                    can = new ControllerAreaNetwork(settings.CanPort, ControllerAreaNetwork.Speed.Kbps250);
                    break;
                case CanAdapterSettings.CanSpeed.Kbps125:
                    can = new ControllerAreaNetwork(settings.CanPort, ControllerAreaNetwork.Speed.Kbps125);
                    break;
                default:
                    can = new ControllerAreaNetwork(settings.CanPort, timings ?? GetTimings(settings));
                    break;
            }
            can.ErrorReceived += Can_ErrorReceived;
            can.MessageAvailable += Can_MessageAvailable;
        }

        public override bool IsEnabled
        {
            get { return can.Enabled; }
            set { can.Enabled = value; }
        }

        private void Can_MessageAvailable(ControllerAreaNetwork sender, ControllerAreaNetwork.MessageAvailableEventArgs e)
        {
            var messages = sender.ReadMessages();
            for(int i =0; i <messages.Length;i++)
            {
                OnMessageReceived(new CanMessage(messages[i]));
            }
        }

        private void Can_ErrorReceived(ControllerAreaNetwork sender, ControllerAreaNetwork.ErrorReceivedEventArgs e)
        {
            if (true) //(e.Error == ControllerAreaNetwork.Error.BusOff)
            {
                can.Reset();
            }
            switch (e.Error)
            {
                case ControllerAreaNetwork.Error.BusOff:
                    OnError("BusOff");
                    break;
                case ControllerAreaNetwork.Error.ErrorPassive:
                    OnError("ErrorPassive");
                    break;
                case ControllerAreaNetwork.Error.Overrun:
                    OnError("Overrun");
                    break;
                case ControllerAreaNetwork.Error.RXOver:
                    OnError("RXOver");
                    break;
                default:
                    OnError("Unknown");
                    break;
            }
        }

        public override bool SendMessage(CanMessage message)
        {
            CheckEnabled();
            if (!can.CanSend)
            {
                throw new CanException("CAN adapter is not allowed to send the message now.");
            }
            var sent = can.SendMessage(message.NativeMessage);
            OnMessageSent(message, sent);
            return sent;
        }
        
        private ControllerAreaNetwork.Timings GetTimings(CanAdapterSettings settings)
        {
            switch (ControllerAreaNetwork.SourceClock)
            {
                case 45000000:
                case 42000000:
                    switch (settings.Speed)
                    {
                        case CanAdapterSettings.CanSpeed.Kbps100:
                            // 21TQ, 66%SP
                            return new ControllerAreaNetwork.Timings(0, 12, 8, 20, 1);
                    }
                    break;
                case 72000000:
                    switch (settings.Speed)
                    {
                        case CanAdapterSettings.CanSpeed.Kbps100:
                            // 24TQ, 65%SP
                            return new ControllerAreaNetwork.Timings(0, 15, 8, 30, 1);
                    }
                    break;
            }
            throw new CanException("Specified baudrate isn't supported for current CAN controller frequency.");
        }
    }
}
