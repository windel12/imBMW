///////////////////////////////////////////////////////////////////////////////
// The MIT License
// 
// Copyright (c) 2011-2013 Mike Jones, Matt Weimer, Niko Mäkitalo
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
///////////////////////////////////////////////////////////////////////////////

// Source code is modified from Matt Weimer's JSON Serialization and Deserialization library (https://github.com/mweimer/Json.NetMF)
// Source code is modified from Mike Jones's JSON Serialization and Deserialization library (https://www.ghielectronics.com/community/codeshare/entry/357)

using System;
using Microsoft.SPOT;
using System.Reflection;
using System.Collections;
using System.Text;

namespace Json.NETMF
{
	/// <summary>
    /// JSON.NetMF - JSON Serialization and Deserialization library for .NET Micro Framework
	/// </summary>
    public class JsonSerializer
	{
        public JsonSerializer(DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
		{
            DateFormat = dateTimeFormat;
		}

	    /// <summary>
	    /// Gets/Sets the format that will be used to display
	    /// and parse dates in the Json data.
	    /// </summary>
        public DateTimeFormat DateFormat { get; set; }

        /// <summary>
        /// Convert an object to a JSON string.
        /// </summary>
        /// <param name="o">The value to convert. Supported types are: Boolean, String, Byte, (U)Int16, (U)Int32, Float, Double, Decimal, Array, IDictionary, IEnumerable, Guid, Datetime, DictionaryEntry, Object and null.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        /// <remarks>For objects, only public properties with getters are converted.</remarks>
		public string Serialize(object o)
		{
            return SerializeObject(o, this.DateFormat);
		}

        /// <summary>
        /// Desrializes a Json string into an object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns>An ArrayList, a Hashtable, a double, a long, a string, null, true, or false</returns>
        public object Deserialize(string json)
        {
            return DeserializeString(json);
        }

		/// <summary>
		/// Deserializes a Json string into an object.
		/// </summary>
		/// <param name="json"></param>
        /// <returns>An ArrayList, a Hashtable, a double, a long, a string, null, true, or false</returns>
		public static object DeserializeString(string json)
		{
			return JsonParser.JsonDecode(json);
		}

        /// <summary>
        /// Convert an object to a JSON string.
        /// </summary>
        /// <param name="o">The value to convert. Supported types are: Boolean, String, Byte, (U)Int16, (U)Int32, Float, Double, Decimal, Array, IDictionary, IEnumerable, Guid, Datetime, DictionaryEntry, Object and null.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        /// <remarks>For objects, only public properties with getters are converted.</remarks>
        public static string SerializeObject(object o, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            if (o == null)
                return "null";

            Type type = o.GetType();

            switch (type.Name)
            {
                case "Boolean":
                    {
                        return (bool)o ? "true" : "false";
                    }
                case "String":
                case "Char":
                case "Guid":
                    {
                        return "\"" + o.ToString() + "\"";
                    }
                case "Single":
                case "Double":
                case "Decimal":
                case "Float":
                case "Byte":
                case "SByte":
                case "Int16":
                case "UInt16":
                case "Int32":
                case "UInt32":
                case "Int64":
                case "UInt64":
                    {
                        return o.ToString();
                    }
                case "DateTime":
                    {
                        switch (dateTimeFormat)
                        {
                            case DateTimeFormat.Ajax:
                                // This MSDN page describes the problem with JSON dates:
                                // http://msdn.microsoft.com/en-us/library/bb299886.aspx
                                return "\"" + DateTimeExtensions.ToASPNetAjax((DateTime)o) + "\"";
                            case DateTimeFormat.ISO8601:
                            case DateTimeFormat.Default:
                            default:
                                return "\"" + DateTimeExtensions.ToIso8601((DateTime)o) + "\"";
                        }
                    }
            }

            if (o is IDictionary && !type.IsArray)
            {
                IDictionary dictionary = o as IDictionary;
                return SerializeIDictionary(dictionary, dateTimeFormat);
            }

            if (o is IEnumerable)
            {
                IEnumerable enumerable = o as IEnumerable;
                return SerializeIEnumerable(enumerable, dateTimeFormat);
            }

            if (type == typeof(System.Collections.DictionaryEntry))
            {
                DictionaryEntry entry = o as DictionaryEntry;
                Hashtable hashtable = new Hashtable();
                hashtable.Add(entry.Key, entry.Value);
                return SerializeIDictionary(hashtable, dateTimeFormat);
            }

            if (type.IsClass)
            {
                Hashtable hashtable = new Hashtable();

                // Iterate through all of the methods, looking for public GET properties
                MethodInfo[] methods = type.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    // We care only about property getters when serializing
                    if (method.Name.StartsWith("get_"))
                    {
                        // Ignore abstract and virtual objects
                        if (method.IsAbstract)
                        {
                            continue;
                        }

                        // Ignore delegates and MethodInfos
                        if ((method.ReturnType == typeof(System.Delegate)) ||
                            (method.ReturnType == typeof(System.MulticastDelegate)) ||
                            (method.ReturnType == typeof(System.Reflection.MethodInfo)))
                        {
                            continue;
                        }
                        // Ditto for DeclaringType
                        if ((method.DeclaringType == typeof(System.Delegate)) ||
                            (method.DeclaringType == typeof(System.MulticastDelegate)))
                        {
                            continue;
                        }

                        object returnObject = method.Invoke(o, null);
                        hashtable.Add(method.Name.Substring(4), returnObject);                 
                    }
                }
                return SerializeIDictionary(hashtable, dateTimeFormat);
            }

            return null;
        }

        /// <summary>
        /// Convert an IEnumerable to a JSON string.
        /// </summary>
        /// <param name="enumerable">The value to convert.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        protected static string SerializeIEnumerable(IEnumerable enumerable, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            StringBuilder result = new StringBuilder("[");
            
            int Count = 0;
            foreach (object current in enumerable) { Count++; }

            int j = 0;
            foreach (object current in enumerable)
            {
                result.Append(SerializeObject(current, dateTimeFormat));
                if (result.Length > 0)
                {
                    if(j < Count - 1) 
                    {
                        result.Append(",");
                    }
                }
                ++j;
            }

            result.Append("]");
            return result.ToString();
        }

        /// <summary>
        /// Convert an IDictionary to a JSON string.
        /// </summary>
        /// <param name="dictionary">The value to convert.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        protected static string SerializeIDictionary(IDictionary dictionary, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            StringBuilder result = new StringBuilder("{");

            int i = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                result.Append("\"" + entry.Key + "\"");
                result.Append(":");
                result.Append(SerializeObject(entry.Value, dateTimeFormat));
                if (result.Length > 0)
                {
                    if(i < dictionary.Count - 1) {
                        result.Append(",");
                    }
                }
                ++i;
            }

            result.Append("}");
            return result.ToString();
        }

	}

    /// <summary>
    /// Enumeration of the popular formats of time and date
    /// within Json.  It's not a standard, so you have to
    /// know which on you're using.
    /// </summary>
    public enum DateTimeFormat
    {
        Default = 0,
        ISO8601 = 1,
        Ajax = 2
    }
}
