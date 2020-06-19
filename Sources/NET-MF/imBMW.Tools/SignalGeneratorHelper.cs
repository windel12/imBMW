using System;
using System.Collections;
using GHI.IO;

namespace imBMW.Tools
{
    public static class SignalGeneratorHelper
    {
        public static uint[] Set(this SignalGenerator signalGenerator, bool initialValue, byte[] bytes, uint delay, bool repeat)
        {
            var timingArray = new ArrayList();

            bool prevValue = initialValue;
            bool currentValue = false;
            uint timingValue = 0;

            for (byte i = 0; i < bytes.Length; i++)
            {
                var _byte = bytes[i];

                for (byte j = 0; j < 9; j++)
                {
                    if (j == 0)
                    {
                        timingValue += 104;
                        prevValue = false;
                    }

                    currentValue = j <= 7 ? _byte.HasBit(j) : isParity(_byte);
                    if (currentValue == prevValue)
                    {
                        timingValue += 104;
                        prevValue = currentValue;
                    }
                    else
                    {
                        timingArray.Add(timingValue);
                        timingValue = 104;
                        prevValue = currentValue;
                    }

                    if (j == 8)
                    {
                        if (currentValue)
                        {
                            timingValue = timingValue + (i != bytes.Length - 1 ? delay : 0);
                            timingArray.Add(timingValue);
                        }
                        else
                        {
                            timingArray.Add(timingValue);
                            if (i != bytes.Length - 1)
                            {
                                timingArray.Add(delay);
                            }
                        }
                        timingValue = 0;
                    }
                }
            }


            var timingBuffer = new uint[timingArray.Count];
            for (int i = 0; i < timingBuffer.Length; i++)
            {
                timingBuffer[i] = (uint)timingArray[i];
            }
            signalGenerator.Set(initialValue, timingBuffer, repeat);
            return timingBuffer;
        }

        static bool isParity(byte x)
        {
            int y = x ^ (x >> 1);
            y = y ^ (y >> 2);
            y = y ^ (y >> 4);
            y = y ^ (y >> 8);
            y = y ^ (y >> 16);

            // Rightmost bit of y holds 
            // the parity value 
            // if (y&1) is 1 then parity  
            // is odd else even 
            if ((y & 1) > 0)
                return true;
            return false;
        }
    }
}
