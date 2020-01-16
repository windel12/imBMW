///////////////////////////////////////////////////////////////////////////////
//	Copyright 2013 JASDev International
//
//	Licensed under the Apache License, Version 2.0 (the "License");
//	you may not use this file except in compliance with the License.
//	You may obtain a copy of the License at
//
//		http://www.apache.org/licenses/LICENSE-2.0
//
//	Unless required by applicable law or agreed to in writing, software
//	distributed under the License is distributed on an "AS IS" BASIS,
//	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	See the License for the specific language governing permissions and
//	limitations under the License.
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;

namespace JDI.Common
{
	public class TimerEx : IDisposable
	{
		public TimerEx()
		{
			this.syncObject = new object();
			this.timedOut = false;
			this.timeoutTime = Timeout.Infinite;
			this.state = null;
			this.timer = null;
		}

		public void Dispose()
		{

		}

		public object State
		{
			get
			{
				object temp = null;
				lock (this.syncObject)
				{
					temp = this.state;
				}
				return temp;
			}
		}

		/// <summary>
		/// Returns true if the timer has timed out.
		/// </summary>
		public bool HasTimedOut
		{
			get
			{
				bool temp = false;
				lock (this.syncObject)
				{
					temp = this.timedOut;
				}
				return temp;
			}
		}

		/// <summary>
		/// Starts the timer.
		/// </summary>
		/// <param name="timeoutTime">Number of seconds until timer times out.</param>
		/// <param name="state">Application data associated with the timer.</param>
		public void Start(int timeoutTime, object state)
		{
			lock (this.syncObject)
			{
				if (this.timer != null)
					return;
				this.timedOut = false;
				this.timeoutTime = timeoutTime * JDIConst.MillisecondsPerSecond;
				this.state = state;
				this.timer = new Timer(new TimerCallback(this.timerCallback), this.state, this.timeoutTime, Timeout.Infinite);
			}
		}

		/// <summary>
		/// Restarts the timer using the previous timeout value.
		/// </summary>
		/// <param name="state">Application data associated with the timer.</param>
		public void Restart(object state)
		{
			lock (this.syncObject)
			{
				if (this.timer != null)
				{
					this.timer.Dispose();
					this.timer = null;
				}
				this.timedOut = false;
				this.state = state;
				this.timer = new Timer(new TimerCallback(this.timerCallback), this.state, this.timeoutTime, Timeout.Infinite);
			}
		}

		/// <summary>
		/// Stops the timer.
		/// </summary>
		public void Stop()
		{
			lock (this.syncObject)
			{
				if (this.timer != null)
				{
					this.timer.Dispose();
					this.timer = null;
				}
				this.timedOut = false;
				this.timeoutTime = Timeout.Infinite;
				this.state = null;
			}
		}

		private void timerCallback(object data)
		{
			lock (this.syncObject)
			{
				if (this.timer != null)
				{
					this.timer.Dispose();
					this.timer = null;
				}
				this.timedOut = true;
			}
		}

		private object syncObject;
		private bool timedOut;
		private int timeoutTime;
		private object state;
		private Timer timer;
	}
}
