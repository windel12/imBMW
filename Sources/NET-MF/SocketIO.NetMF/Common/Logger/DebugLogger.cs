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

using JDI.Common.Utils;

namespace JDI.Common.Logger
{
	/// <summary>
	/// DebugLogger class
	/// </summary>
	/// <remarks>Implements a Debug (Output) window based data logger for use in the Logger static class.</remarks>
	public class DebugLogger : ILogger
	{
		public void Dispose()
		{
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
			DebugEx.WriteLine(message);
		}
	}
}
