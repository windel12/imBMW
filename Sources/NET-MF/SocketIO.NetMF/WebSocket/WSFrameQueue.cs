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
using System.Collections;
using System.Text;

namespace JDI.WebSocket.Client
{
	/// <summary>
	/// Represents a thread-safe FIFO WSFrame queue.
	/// </summary>
	internal class WSFrameQueue : IDisposable
	{
		#region Constructors

		public WSFrameQueue(int maxCount)
		{
			this.maxCount = maxCount;
			this.queue = new Queue();
		}

		public void Dispose()
		{
			if (this.queue != null)
			{
				this.queue.Clear();
				this.queue = null;
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the number of WSFrames contained in the queue.
		/// </summary>
		public int Count
		{
			get
			{
				int count = 0;
				lock (this.queue.SyncRoot)
				{
					count = this.queue.Count;
				}
				return count;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Adds a WSFrame to the bottom of the queue.
		/// </summary>
		/// <param name="wsFrame">The WSFrame to add to the queue.</param>
		/// <remarks>If Count == MaxCount, then the WSFrame at the top of the queue is removed and discarded before the new entry is added.</remarks>
		public void Enqueue(WSFrame wsFrame)
		{
			lock (this.queue.SyncRoot)
			{
				if (this.queue.Count == this.maxCount)
				{
					this.queue.Dequeue();
				}
				this.queue.Enqueue(wsFrame);
			}
		}

		/// <summary>
		/// Removes the WSFrame at the top of the queue, and returns it.
		/// </summary>
		/// <returns>The WSFrame that is removed from the top of the queue.</returns>
		public WSFrame Dequeue()
		{
			WSFrame wsFrame = null;
			lock (this.queue.SyncRoot)
			{
				wsFrame = (WSFrame)this.queue.Dequeue();
			}
			return wsFrame;
		}

		/// <summary>
		/// Returns the WSFrame at the top of the queue without removing it.
		/// </summary>
		/// <returns>The WSFrame at the top of the queue.</returns>
		public WSFrame Peek()
		{
			WSFrame wsFrame = null;
			lock (this.queue.SyncRoot)
			{
				wsFrame = (WSFrame)this.queue.Peek();
			}
			return wsFrame;
		}

		/// <summary>
		/// Inserts a WSFrame at the top of the queue.
		/// </summary>
		/// <param name="wsFrame">The WSFrame to add to the queue.</param>
		/// <remarks>May be used to insert high-priority messages at the top of the queue.</remarks>
		public void Poke(WSFrame wsFrame)
		{
			lock (this.queue.SyncRoot)
			{
				if (this.queue.Count > this.maxCount)
				{
					this.queue.Dequeue();
				}
				int count = this.queue.Count;
				this.queue.Enqueue(wsFrame);
				while (count > 0)
				{
					this.queue.Enqueue(this.queue.Dequeue());
				}
			}
		}

		/// <summary>
		/// Removes all WSFrames from the queue.
		/// </summary>
		public void Clear()
		{
			lock (this.queue.SyncRoot)
			{
				this.queue.Clear();
			}
		}

		#endregion

		#region Member Fields

		private int maxCount;
		private Queue queue;

		#endregion
	}
}
