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
using System.IO;
using System.Net.Sockets;

using Microsoft.SPOT.Net.Security;

namespace JDI.Common.Net
{
	public class SslStreamEx : Stream
	{
		#region Constructors and IDispose

		public SslStreamEx(Socket socket)
		{
			this.sslStream = new SslStream(socket);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				this.sslStream.Dispose();
			}

			this.sslStream = null;
		}

		#endregion


		#region Stream Implementation

		#region Properties

		public override bool CanRead
		{
			get { return this.sslStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return this.sslStream.CanSeek; }
		}

		public override bool CanTimeout
		{
			get { return this.sslStream.CanTimeout; }
		}

		public override bool CanWrite
		{
			get { return this.sslStream.CanWrite; }
		}

		public override long Length
		{
			get { return this.sslStream.Length; }
		}

		public override long Position
		{
			get { return this.sslStream.Position; }
			set { this.sslStream.Position = value; }
		}

		public override int ReadTimeout
		{
			get { return this.sslStream.ReadTimeout; }
			set { this.sslStream.ReadTimeout = value; }
		}

		public override int WriteTimeout
		{
			get { return this.sslStream.WriteTimeout; }
			set { this.sslStream.WriteTimeout = value; }
		}

		#endregion

		#region Methods

		public override void Flush()
		{
			this.sslStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return this.sslStream.Read(buffer, offset, count);
		}

		public override int ReadByte()
		{
			return this.sslStream.ReadByte();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return this.sslStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			this.sslStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.sslStream.Write(buffer, offset, count);
		}

		public override void WriteByte(byte value)
		{
			this.sslStream.WriteByte(value);
		}

		#endregion

		#endregion


		#region SslStream Implementation

		#region Properties

		public bool IsServer
		{
			get { return this.sslStream.IsServer; }
		}

		#endregion


		#region Methods

		public void AuthenticateAsClient(string targetHost)
		{
			this.sslStream.AuthenticateAsClient(targetHost);
		}

		#endregion

		#endregion


		#region Member Fields

		private SslStream sslStream;

		#endregion
	}
}
