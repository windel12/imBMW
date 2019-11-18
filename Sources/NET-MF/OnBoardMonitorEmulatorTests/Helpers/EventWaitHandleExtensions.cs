using System.Diagnostics;
using System.Threading;

namespace OnBoardMonitorEmulatorTests.Helpers
{
    public static class EventWaitHandleExtensions
    {
        public static bool Wait(this EventWaitHandle waitHandle, int timeout = 1000)
        {
            return waitHandle.WaitOne(Debugger.IsAttached ? 30000 : timeout);
        }
    }
}
