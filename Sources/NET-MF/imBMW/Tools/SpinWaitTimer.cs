using System;
using Microsoft.SPOT;

namespace imBMW.Tools
{
    public class SpinWaitTimer
    {
        double _cyclesPerMilliSecond = 344;

        public double CyclesPerMilliSecond
        {
            get { return _cyclesPerMilliSecond; }
            set { _cyclesPerMilliSecond = value; }
        }

        public void Calibrate()
        {
            const int CYCLE_COUNT = 10485;
            int dummyValue = 0;
            DateTime startTime = DateTime.Now;
            for (int i = 0; i < CYCLE_COUNT; ++i)
            {
                ++dummyValue;
            }
            DateTime endTime = DateTime.Now;

            TimeSpan timeDifference = endTime.Subtract(startTime);

            _cyclesPerMilliSecond = ((double)CYCLE_COUNT / (double)timeDifference.Milliseconds);
        }

        public void WaitMilliseconds(double milliseconds)
        {
            int cycleCount = (int)(CyclesPerMilliSecond * milliseconds);
            int dummyValue = 0;
            for (int i = 0; i < cycleCount; ++i)
            {
                ++dummyValue;
            }
        }
    }
}
