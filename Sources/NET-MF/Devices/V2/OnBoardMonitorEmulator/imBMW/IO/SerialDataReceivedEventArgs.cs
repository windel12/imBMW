using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Ports
{
    public delegate void SerialDataReceivedEventHandler(object sender, SerialDataReceivedEventArgs e);

    public class SerialDataReceivedEventArgs
    {
    }
}
