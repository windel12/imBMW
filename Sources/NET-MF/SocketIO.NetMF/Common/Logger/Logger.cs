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

namespace JDI.Common.Logger
{
	public static class Logger
	{
		public static void Initialize(ILogger newLogger, LogLevel logLevel)
		{
			if (logger != null)
			{
				throw new InvalidOperationException("Logger may only be initialized once.");
			}
			logger = newLogger;
			maxLogLevel = logLevel;
		}

		public static LogLevel GetLogLevel()
		{
			return maxLogLevel;
		}

		public static void SetLogLevel(LogLevel logLevel)
		{
			lock (lockObject)
			{
				maxLogLevel = logLevel;
			}
		}

		public static void WriteError(string source, string message, string stackTrace = "")
		{
			lock (lockObject)
			{
				if ((int)maxLogLevel >= (int)LogLevel.Error)
					logger.WriteError(source, message, stackTrace);
			}
		}

		public static void WriteInfo(string source, string message)
		{
			lock (lockObject)
			{
				if ((int)maxLogLevel >= (int)LogLevel.Info)
					logger.WriteInfo(source, message);
			}
		}

		public static void WriteDebug(string source, string message)
		{
			lock (lockObject)
			{
				if ((int)maxLogLevel >= (int)LogLevel.Debug)
					logger.WriteDebug(source, message);
			}
		}

		private static object lockObject = new object();
		private static ILogger logger = null;
		private static LogLevel maxLogLevel = LogLevel.Error;
	}
}
