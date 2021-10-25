using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Voting2021.BlockchainClient
{
	public static class Base58
	{
		private static readonly char[] _chars;

		private static readonly Dictionary<char, int> _inverseSearch = new();

		static Base58()
		{
			var space = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
			_chars = new char[space.Length];
			for (int i = 0; i < space.Length; i++)
			{
				var ch = space[i];
				_chars[i] = ch;
				_inverseSearch.Add(ch, i);
			}
		}

		public static string EncodePlain(byte[] data)
		{
			var intData = data.Aggregate<byte, BigInteger>(0, (current, t) => current * 256 + t);

			var result = string.Empty;
			while (intData > 0)
			{
				var remainder = (int) (intData % 58);
				intData /= 58;
				result = _chars[remainder] + result;
			}

			for (var i = 0; i < data.Length && data[i] == 0; i++)
			{
				result = '1' + result;
			}

			return result;
		}

		public static byte[] DecodePlain(string data)
		{
			// Decode Base58 string to BigInteger 
			BigInteger intData = 0;
			for (var i = 0; i < data.Length; i++)
			{
				var ch = data[i];
				if (!_inverseSearch.TryGetValue(ch, out var pos))
				{
					throw new FormatException(string.Format("Invalid Base58 character `{0}` at position {1}", data[i], i));
				}

				intData = intData * 58 + pos;
			}

			// Encode BigInteger to byte[]
			// Leading zero bytes get encoded as leading `1` characters
			var leadingZeroCount = data.TakeWhile(c => c == '1').Count();
			var leadingZeros = Enumerable.Repeat((byte) 0, leadingZeroCount);
			var bytesWithoutLeadingZeros =
			  intData.ToByteArray()
			  .Reverse()// to big endian
			  .SkipWhile(b => b == 0);//strip sign byte
			var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();

			return result;
		}
	}
}
