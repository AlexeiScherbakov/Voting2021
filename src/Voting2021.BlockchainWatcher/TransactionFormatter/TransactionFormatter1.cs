using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Voting2021.BlockchainWatcher.Services
{

	public sealed class TransactionFormatter1
		: TransactionFormatter
	{
		public override byte[] WriteTransaction(string key, byte[] value, Dictionary<string, string> headers)
		{
			var m = new MemoryStream(4096);
			var binaryWriter = new BinaryWriter(m);
			binaryWriter.Write((byte) 0xCA);
			binaryWriter.Write((byte) 0xFE);
			binaryWriter.Write((byte) 0xBA);
			if (value is null)
			{
				if (headers is null)
				{
					binaryWriter.Write((byte) 0xBD);
					binaryWriter.Flush();
				}
				else
				{
					binaryWriter.Write((byte) 0xBC);
					binaryWriter.Flush();
					Utf8JsonWriter writer = new Utf8JsonWriter(m);
					JsonSerializer.Serialize(writer, headers);
				}
			}
			else
			{
				if (headers is null)
				{
					binaryWriter.Write((byte) 0xBF);
					binaryWriter.Write7BitEncodedInt(value.Length);
					binaryWriter.Flush();
					m.Write(value);
				}
				else
				{
					binaryWriter.Write((byte) 0xBE);
					binaryWriter.Write7BitEncodedInt(value.Length);
					binaryWriter.Flush();
					m.Write(value);
					Utf8JsonWriter writer = new Utf8JsonWriter(m);
					JsonSerializer.Serialize(writer, headers);
				}
			}
			m.Flush();
			return m.ToArray();
		}
	}
}
