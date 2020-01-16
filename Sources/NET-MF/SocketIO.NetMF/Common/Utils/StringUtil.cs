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

namespace JDI.Common.Utils
{
	public static class StringUtil
	{
		/// <summary>
		/// Indicates whether the specified string is null or an Empty string.
		/// </summary>
		/// <param name="s">The string to test.</param>
		/// <returns>Returns true if the value parameter is null or an empty string (""); otherwise, false</returns>
		public static bool IsNullOrEmpty(string value)
		{
			return (value == null || value.Length == 0 ? true : false);
		}
	}
}
