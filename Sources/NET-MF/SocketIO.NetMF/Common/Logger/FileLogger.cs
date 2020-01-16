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

using JDI.Common.Extensions;
using JDI.Common.Utils;

namespace JDI.Common.Logger
{
	/// <summary>
	/// FileLogger class
	/// </summary>
	/// <remarks>Implements a file-based data logger for use in the Logger static class.</remarks>
	public class FileLogger : ILogger
	{
		public FileLogger()
		{
			this.isDisposed = false;
			this.sync = new object();
			this.streamWriter = null;
		}

		~FileLogger()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (this.isDisposed)
				return;

			if (disposing)
			{
				if (this.streamWriter != null)
				{
					this.streamWriter.Flush();
					this.streamWriter.Close();
					this.streamWriter.Dispose();
				}
			}

			this.streamWriter = null;
		}

		public void Open(string directory, string fileName, string fileExt="log")
		{
			lock (this.sync)
			{
				if (this.streamWriter != null)
					this.streamWriter.Close();

				string fullPath = null;
				string name = fileName;
				for (int i = 1; i < 100; ++i)
				{
					fileName = string.Concat(name, i.Format("D2"), ".", fileExt);
					fullPath = Path.Combine(directory, fileName);
					if (File.Exists(fullPath))
					{
						if (i == 99)
						{
							fileName = string.Concat(name, "01", ".", fileExt);
							fullPath = Path.Combine(directory, fileName);
							break;
						}
					}
					else
					{
						break;
					}
				}

				if (!StringUtil.IsNullOrEmpty(fullPath))
				{
					Directory.CreateDirectory(directory);
					this.streamWriter = new StreamWriter(File.Create(fullPath));
				}

				if (this.streamWriter != null)
				{
					this.streamWriter.WriteLine(string.Concat("Log file opened: ", DateTime.Now.ToString("MM/dd/yyyy | HH:mm:ss.fff")));
					this.streamWriter.Flush();
				}
			}
		}

		public void Close()
		{
			lock (this.sync)
			{
				if (this.streamWriter != null)
				{
					this.streamWriter.Flush();
					this.streamWriter.Close();
					this.streamWriter = null;
				}
			}
		}

		public void WriteError(string source, string message, string stackTrace = "")
		{
			this.WriteToLog(string.Concat("Error | ", DateTime.Now.ToString("MM/dd/yyyy | HH:mm:ss.fff"), " | ", source, " | ", message, " | ", stackTrace));
		}

		public void WriteInfo(string source, string message)
		{
			this.WriteToLog(string.Concat("Info  | ", DateTime.Now.ToString("MM/dd/yyyy | HH:mm:ss.fff"), " | ", source, " | ", message));
		}

		public void WriteDebug(string source, string message)
		{
			this.WriteToLog(string.Concat("Debug | ", DateTime.Now.ToString("MM/dd/yyyy | HH:mm:ss.fff"), " | ", source, " | ", message));
		}

		private void WriteToLog(string message)
		{
			lock (this.sync)
			{
				if (this.streamWriter != null)
				{
					this.streamWriter.WriteLine(message);
					this.streamWriter.Flush();
				}
			}
		}

		private bool isDisposed;
		private object sync = null;
		private StreamWriter streamWriter = null;
	}
}
