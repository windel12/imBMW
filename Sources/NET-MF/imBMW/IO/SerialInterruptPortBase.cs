using System;
using Microsoft.SPOT;
using System.Threading;

namespace System.IO.Ports
{
    public abstract class SerialInterruptPortBase : SerialPortBase, ISerialPort, IDisposable
    {
        protected SerialPort _port; // The actual serial port we are wrapping.
        
        /// <summary>
        /// Creates a new instance of SerialInterruptPort class, allowing to specify buffer sizes.
        /// </summary>
        /// <param name="config">An object that contains the configuration information for the serial port.</param>
        /// <param name="writeBufferSize">The size of output buffer in bytes. Data output is paused for <see cref="AfterWriteDelay"/> milliseconds every time this amount of data is sent. Can be zero to disable pausing.</param>
        /// <param name="readBufferSize">The size of input buffer in bytes. DataReceived event will fire only after this amount of data is received. Default is 1.</param>
        /// <param name="readTimeout">Timeout of port reading.</param>
        public SerialInterruptPortBase(SerialPortConfiguration config, int writeBufferSize, int readBufferSize, int readTimeout = Timeout.Infinite)
            : base(writeBufferSize, readBufferSize)
        {
            _port = new SerialPort(config.PortName, (int)config.BaudRate, config.Parity, config.DataBits, config.StopBits)
            {
                Handshake = config.HardwareFlowControl ? Handshake.RequestToSend : Handshake.None
            };
            
            AfterWriteDelay = 33;
            ReadTimeout = readTimeout;

#if NETMF
            _port.Open();
#else
// in this case, need to open port manually in code(e.g. ActivateScreen.cs as link in OnBoardMonitorEmulator)
#endif
        }

        /// <summary>
        /// Creates a new instance of SerialInterruptPort class, with hardware flow control and output pausing disabled. This corresponds to standard <see cref="SerialPort"/> class behavior.
        /// </summary>
        /// <param name="config">An object that contains the configuration information for the serial port.</param>
        public SerialInterruptPortBase(SerialPortConfiguration config) : this(config, 0, 1) { }

        /// <summary>
        /// Releases resources used by a serial port.
        /// </summary>
        public virtual void Dispose()
        {
            if (_port != null) _port.Dispose(); // release the serial port if applicable
        }

        protected override void OnBusyChanged(bool busy)
        {
            // currently not writing
            if (_writeThread != null)
            {
                if (busy) _writeThread.Suspend(); // if busy was set, pause sending the data
                else _writeThread.Resume();       // if it was cleared, resume sending the data
            }

            base.OnBusyChanged(busy);
        }

        protected override int WriteDirect(byte[] data, int offset, int length)
        {
            _port.Write(data, offset, length);
            return length;
        }

        /// <summary>
        /// Empties the contents of a serial port's buffer.
        /// </summary>
        public override void Flush()
        {
#if NETMF
            _port.Flush();
#endif
        }

        public override string Name
        {
            get { return _port.PortName;  }
        }

        public override void Open()
        {
            _port.Open();
        }

        public override void Close()
        {
            _port.Close();
        }

        public override bool IsOpen
        {
            get { return _port.IsOpen; }
        }

        public override int ReadTimeout
        {
            get
            {
                return base.ReadTimeout;
            }
            set
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                    _port.ReadTimeout = value;
                    _port.Open();
                }
                else
                {
                    _port.ReadTimeout = value;
                }
                base.ReadTimeout = value;
            }
        }

        protected override int ReadDirect(byte[] data, int offset, int length)
        {
            return _port.Read(data, offset, length);
        }
    }
}
