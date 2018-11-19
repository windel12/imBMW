using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SPOT.Hardware;

namespace System.IO.Ports
{
    public abstract class SerialPortBase : ISerialPort
    {
        protected Thread _writeThread;                // Thread which is sending data out. This is usually a calling thread.
        protected int _writeBufferSize;               // Size of the output buffer, which can be used to insert pauses between some amount of data.

        protected Thread _readThread;                 // Thread which is reading the data when DataReceived event is being requested. We create and dispose this thread inside the class.
        protected int _readBufferSize;                // Size of the input buffer. The DataReceived event won't fire until this amount of bytes comes in.

        public SerialPortBase() : this(0, 1) { }

        public SerialPortBase(int writeBufferSize, int readBufferSize)
        {
            // some initial parameter checks.
            if (writeBufferSize < 0) throw new ArgumentOutOfRangeException("writeBufferSize");
            if (readBufferSize < 1) throw new ArgumentOutOfRangeException("readBuferSize");

            _writeBufferSize = writeBufferSize;
            _readBufferSize = readBufferSize;

            //_bufferSync = new object();                 // initializing the sync root object
            //_incomingBuffer = new byte[_readBufferSize]; // allocating memory for incoming data
        }

        protected byte[] data;

        protected abstract bool CanWrite { get; }

        protected abstract int WriteDirect(byte[] data, int offset, int length);

        public virtual void Write(byte[] data, int offset, int length)
        {
            _writeThread = Thread.CurrentThread;                                // grab the current thread so that we can pause the writing
            while (!CanWrite) _writeThread.Suspend();                              // do not continue if _busy is already set (eg. the signal was changed when we weren't writing)

            if (_writeBufferSize < 1)                                           // If user does not want to split data into chunks,
            {
                WriteDirect(data, 0, data.Length);                              // pass it to the SerialPort output without change.
                _writeThread = null;                                            // release current thread so that the _busy signal does not affect external code execution
                return;
            }

            int modulus = length % _writeBufferSize;                            // prepare data which fill the _writeBufferSize completely
            length -= modulus;                                                  // (If there is not enough data to fill it, length would be zero after this line,

            for (int i = offset; i < offset + length; i += _writeBufferSize)    // and this cycle would not execute.)
            {
                WriteDirect(data, i, _writeBufferSize);                         // send it out
                if (AfterWriteDelay > 0) Thread.Sleep(AfterWriteDelay);         // and include pause after chunk
            }

            if (modulus > 0)                                                    // If any data left which do not fill whole _writeBuferSize chunk,
            {
                WriteDirect(data, offset + length, modulus);                    // send it out as well
                if (AfterWriteDelay > 0) Thread.Sleep(AfterWriteDelay);         // and pause for case consecutive calls to any write method.
            }

            _writeThread = null;                                                // release current thread so that the _busy signal does not affect external code execution
        }

        public void Write(params byte[] data)
        {
            Write(data, 0, data.Length);
        }

        public void Write(string text)
        {
        }

        public void WriteLine(string text)
        {
        }


        protected abstract int ReadDirect(byte[] data, int offset, int length);

        public int AvailableBytes => data.Length;

        public virtual int AfterWriteDelay { get; set; }

        public virtual int ReadTimeout { get; set; }

        public byte[] ReadAvailable() => data;

        public byte[] ReadAvailable(int maxCount)
        {
            return ReadAvailable();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return data.Length;
        }

        public string ReadLine()
        {
            return "";
        }

        protected virtual void OnBusyChanged(bool busy)
        {
            var e = BusyChanged;
            if (e != null)
            {
                e(busy);
            }
        }

        protected virtual void OnDataReceived(SerialDataReceivedEventArgs args)
        {
            var e = DataReceived;
            if (e != null)
            {
                e(this, args);
            }
        }

        public abstract void Flush();

        public abstract bool IsOpen { get; }

        public abstract void Open();

        public virtual event BusyChangedEventHandler BusyChanged;

        public virtual event SerialDataReceivedEventHandler DataReceived;
    }
}
