using System;

namespace imBMW
{
    public delegate void Action();
    public delegate void ActionByte(byte value);
    public delegate void ActionString(string value);
    public delegate void ActionDouble(double value);
    public delegate void ActionException(Exception ex);
}
