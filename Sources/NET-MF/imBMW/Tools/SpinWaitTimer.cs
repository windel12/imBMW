//using System;
//using Microsoft.SPOT;

//namespace imBMW.Tools
//{
//    public class SpinWaitTimer
//    {
//        double _cyclesPerSecond = 112262.2255516001;

//        public double CyclesPerSecond
//        {
//            get { return _cyclesPerSecond; }
//            set { _cyclesPerSecond = value; }
//        }

//        public void Calibrate()
//        {
//            const int CYCLE_COUNT = 104857;
//            int dummyValue = 0;
//            DateTime startTime = DateTime.Now;
//            for (int i = 0; i < CYCLE_COUNT; ++i)
//            {
//                ++dummyValue;
//            }
//            DateTime endTime = DateTime.Now;

//            TimeSpan timeDifference = endTime.Subtract(startTime);

//            _cyclesPerSecond = ((double)CYCLE_COUNT / (double)timeDifference.Ticks) * 10000000d;
//        }

//        public void WaitMilliseconds(double microseconds)
//        {
//            int cycleCount = (int)(_cyclesPerSecond * CyclesPerSecond / 1000d);
//            int dummyValue = 0;
//            for (int i = 0; i < cycleCount; ++i)
//            {
//                ++dummyValue;
//            }
//        }
//    }
//}
