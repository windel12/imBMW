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
using System.Security.Cryptography;

namespace JDI.Common.Security
{
	public static class CryptoUtils
	{
		/// <summary>
		/// Creates an array of bytes with a cryptographically strong sequence of random values.
		/// </summary>
		/// <param name="length">Length of array to create.</param>
		/// <returns>An array of bytes filled with a cryptographically strong sequence of random values.</returns>
		public static byte[] GetRandomBytes(int length)
		{
			byte[] randomBytes = new byte[length];
			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
			rng.GetBytes(randomBytes);
			return randomBytes;
		}

		/// <summary>
		/// Computes the Sha1 hash value for the specified byte array.
		/// </summary>
		/// <param name="input">The byte-array to compute the hash value for.</param>
		/// <returns>The computed hash value.</returns>
		public static byte[] ComputeSha1Hash(byte[] input)
		{
			byte[] hash = null;
			using (HashAlgorithm sha1 = new HashAlgorithm(HashAlgorithmType.SHA1))
			{
				sha1.Initialize();
				hash = sha1.ComputeHash(input);
			}
			return hash;
		}
	}
}
