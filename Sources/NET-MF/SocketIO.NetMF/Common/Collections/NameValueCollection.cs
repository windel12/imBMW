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

namespace JDI.Common.Collections
{
	public interface INameValueEnumerator : IEnumerator
	{
	}


	/// <summary>
	/// Represents a collection of name/value pairs that are organized based on the hash code of the name.
	/// </summary>
	public class NameValueCollection : ICollection
	{
		public NameValueCollection()
		{
			this.hashTable = new Hashtable();
		}


		#region IDictionary

		/// <summary>
		/// Gets or sets the value of the element with the specified name.
		/// </summary>
		/// <param name="name">The name of the element to get or set.</param>
		/// <returns>The value of the element with the specified name, or null if the name does not exist.</returns>
		public string this[string name]
		{
			get { return (this.hashTable.Contains("name") ? (string)this.hashTable[name] : null); }
			set { this.hashTable[name] = value; }
		}

		/// <summary>
		/// Adds an element with the provided name and value to the collection.
		/// </summary>
		/// <param name="name">The name of the element to add.</param>
		/// <param name="value">The value of the element to add.</param>
		public void Add(string name, string value)
		{
			this.hashTable[name] = value;
		}

		/// <summary>
		/// Removes all items from the collection.
		/// </summary>
		public void Clear()
		{
			this.hashTable.Clear();
		}

		/// <summary>
		/// Determines whether the collection contains an item with the specified name.
		/// </summary>
		/// <param name="name">The name of the item to search for.</param>
		/// <returns>true if the specified name was found in the collection; otherwise, false.</returns>
		public bool Contains(string name)
		{
			return this.hashTable.Contains(name);
		}

		/// <summary>
		/// Removes the item with the specified name from the collection.
		/// </summary>
		/// <param name="name">The name of the item to remove.</param>
		public void Remove(string name)
		{
			this.hashTable.Remove(name);
		}

		#endregion


		#region ICollection

		/// <summary>
		/// Gets a value indicating whether access to the collection is synchronized (thread safe).
		/// </summary>
		public bool IsSynchronized
		{
			get { return false; }
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the collection.
		/// </summary>
		public object SyncRoot
		{
			get { return this.hashTable.SyncRoot; }
		}

		/// <summary>
		/// Gets the number of name/value pairs contained in the Hashtable
		/// </summary>
		public int Count
		{
			get { return this.hashTable.Count; }
		}

		/// <summary>
		/// Copies the hashtable elements to a NameValuePair array at the specified index.
		/// </summary>
		/// <param name="array">The NameValuePair array that is the destination of the hashtable items.</param>
		/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
		public void CopyTo(NameValuePair[] typedArray, int arrayIndex)
		{
			//var typedArray = array;

			if (typedArray == null)
				throw new ArgumentNullException("array");
			if (typedArray == null)
				throw new InvalidCastException("array must be of type NameValuePair[]");
			if (arrayIndex < 0 || (typedArray.Length - arrayIndex) < this.hashTable.Keys.Count)
				throw new ArgumentOutOfRangeException("arrayIndex");

			foreach (object key in this.hashTable.Keys)
			{
				typedArray[arrayIndex++] = new NameValuePair((string)key, (string)this.hashTable[key]);
			}
		}

		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			this.CopyTo((NameValuePair[])array, arrayIndex);
		}

		#endregion


		#region IEnumerable

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
		public INameValueEnumerator GetEnumerator()
		{
			return new NameValueEnumerator(this.hashTable);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		protected class NameValueEnumerator : INameValueEnumerator
		{
			public NameValueEnumerator(Hashtable hashTable)
			{
				this.hashEnumerator = hashTable.GetEnumerator();
			}

			object IEnumerator.Current
			{
				get { return this.Current; }
			}

			public NameValuePair Current
			{
				get
				{
					DictionaryEntry hashEntry = (DictionaryEntry)this.hashEnumerator.Current;
					return new NameValuePair((string)hashEntry.Key, (string)hashEntry.Value);
				}
			}

			public NameValuePair Entry
			{
				get { return this.Current; }
			}

			public string Name
			{
				get { return this.Current.Name; }
			}

			public string Value
			{
				get { return this.Current.Value; }
			}

			public void Reset()
			{
				this.hashEnumerator.Reset();
			}

			public bool MoveNext()
			{
				return this.hashEnumerator.MoveNext();
			}

			protected IEnumerator hashEnumerator;
		}

		#endregion


		protected Hashtable hashTable;
	}
}
