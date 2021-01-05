using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHI.IO
{
    public class ControllerAreaNetwork
    {
        static ControllerAreaNetwork()
        {
            SourceClock = 42000000;
        }

        public ControllerAreaNetwork(int channel, Speed baudRate) { }

        public ControllerAreaNetwork(int channel, Timings timings) { }

        public ControllerAreaNetwork(Channel channel, Speed baudRate) { }

        public ControllerAreaNetwork(Channel channel, Timings timings) { }

        public static int SourceClock { get; }
        public int AvailableMessages { get; }
        public Timings BaudRateTimings { get; set; }
        public bool CanSend { get; }
        public bool Enabled { get; set; }
        public bool IsTransmitBufferEmpty { get; }
        public int ReceiveBufferSize { get; set; }
        public int ReceiveErrorCount { get; }
        public int TransmitErrorCount { get; }
        public Channel UsedChannel { get; }

        public event ErrorReceivedEventHandler ErrorReceived;
        public event MessageAvailableEventHandler MessageAvailable;

        public Message[] ReadMessages() { return new Message[0]; }
        public void Reset() { }
        public bool SendMessage(Message message) { return true; }

        public enum Channel : byte
        {
            One = 1,
            Two = 2
        }

        public enum Error : byte
        {
            Overrun = 0,
            RXOver = 1,
            BusOff = 2,
            ErrorPassive = 3
        }

        public enum Speed
        {
            Kbps33 = 0,
            Kbps83 = 1,
            Kbps125 = 2,
            Kbps250 = 3,
            Kbps500 = 4,
            Kbps1000 = 5
        }

        public class ErrorReceivedEventArgs : EventArgs
        {
            public Error Error { get; }
        }

        public class Message
        {
            public Message() { }

            public Message(uint arbitrationId) { }

            public Message(uint arbitrationId, byte[] data) { }

            public Message(uint arbitrationId, byte[] data, int offset, int count) { }

            public Message(uint arbitrationId, byte[] data, int offset, int count, bool isRTR, bool isEID) { }

            public uint ArbitrationId { get; set; }

            public byte[] Data { get; set; }

            public bool IsExtendedId { get; set; }

            public bool IsRemoteTransmissionRequest { get; set; }

            public int Length { get; set; }

            public DateTime TimeStamp { get; set; }
        }

        public class MessageAvailableEventArgs : EventArgs
        {
            public int MessagesAvailable { get; }
        }

        public class Timings
        {
            public Timings() { }

            public Timings(int propagation, int phase1, int phase2, int brp, int synchronizationJumpWidth) { }

            public Timings(int propagation, int phase1, int phase2, int brp, int synchronizationJumpWidth, bool useMultiBitSampling) { }

            public int Brp { get; set; }

            public int Phase1 { get; set; }

            public int Phase2 { get; set; }

            public int Propagation { get; set; }

            public int SynchronizationJumpWidth { get; set; }
            //
            public bool UseMultiBitSampling { get; set; }
        }

        public delegate void ErrorReceivedEventHandler(ControllerAreaNetwork sender, ErrorReceivedEventArgs e);
        public delegate void MessageAvailableEventHandler(ControllerAreaNetwork sender, MessageAvailableEventArgs e);
    }
}
