using System;
using System.Collections;
using System.Threading;

namespace imBMW.Tools
{
    public class QueueThreadWorker : Queue
    {
        public delegate void ProcessItem(object item);

        Thread queueThread;
        public string threadName;
        ProcessItem processItem;
        object lockObj = new object();

        private ManualResetEvent ewt = new ManualResetEvent(false);

        public QueueThreadWorker(ProcessItem processItem, string threadName = "", ThreadPriority threadPriority = ThreadPriority.AboveNormal, bool postponeStart = false)
        {
            if (processItem == null)
            {
                throw new ArgumentException("processItem is null");
            }
            this.processItem = processItem;
            this.threadName = threadName;
            queueThread = new Thread(queueWorker);
            queueThread.Priority = threadPriority;
#if !NETMF
            queueThread.Name = threadName;
#endif
            if (!postponeStart)
            {
                queueThread.Start();
            }
        }

        void queueWorker()
        {
            object m;
            while (true)
            {
                lock (lockObj)
                {
                    if (Count > 0)
                    {
                        m = Dequeue();
                    }
                    else
                    {
                        m = null;
                        ewt.Set();
                    }
                }
                if (m == null)
                {
                    queueThread.Suspend();
                    //Thread.CurrentThread.Suspend();
                    continue;
                }
                try
                {
                    processItem(m);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "while processing QueueThreadWorker item '" + m.ToString() + "'");
                }
            }
        }

        public override void Enqueue(object item)
        {
            if (item == null)
            {
                throw new ArgumentException("item is null");
            }
            lock (lockObj)
            {
                base.Enqueue(item);
                CheckRunning();
            }
        }

        public void EnqueueArray(params object[] items)
        {
            if (items == null)
            {
                throw new ArgumentException("items is null");
            }
            lock (lockObj)
            {
                foreach (object item in items)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    base.Enqueue(item);
                }
                CheckRunning();
            }
        }

        public void Start()
        {
            queueThread.Start();
        }

        public void CheckRunning()
        {
            /**
             * Warning! Current item may be added to suspended queue and will be processed only on next Enqueue().
             * Tried AutoResetEvent instead of Suspend/Resume but no success because of strange slowness.
             */
            // TODO Check ResetEvent on LDR and LED
            if (queueThread.ThreadState == ThreadState.Suspended || queueThread.ThreadState == ThreadState.SuspendRequested)
            {
                queueThread.Resume();
            }
        }

        public bool WaitTillQueueBeEmpty()
        {
            lock (lockObj)
            {
                if (Count > 0)
                {
                    ewt.Reset();
                }
            }
            bool result = ewt.WaitOne(2000, true);
            return result;
        }
    }
}
