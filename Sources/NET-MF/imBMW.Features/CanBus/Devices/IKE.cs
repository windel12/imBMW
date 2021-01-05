using System;
using imBMW.Features.CanBus.Adapters;
using imBMW.iBus;
using imBMW.Tools;
using Microsoft.SPOT;

namespace imBMW.Features.CanBus.Devices
{
    public static class IKE
    {
        enum QueueCommand
        {
            Test,
        }

        static QueueThreadWorker queueWorker;

        static IKE()
        {
            //queueWorker = new QueueThreadWorker(ProcessQueue);
        }

        public static void Init()
        {
            //CanAdapter.Current.MessageReceived += Can_MessageReceived;
        }

        private static void Can_MessageReceived(CanAdapter can, CanMessage message)
        {
            Logger.Log(LogPriority.Trace, message.ToString());
        }

        private static void ProcessQueue(object item)
        {
            switch ((QueueCommand) item)
            {
                case QueueCommand.Test:
                    return;
            }
        }
    }
}
