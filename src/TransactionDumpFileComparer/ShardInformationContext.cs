using System;
using System.Collections.Generic;
using System.Linq;

using Voting2021.BlockchainClient;
using Voting2021.FilesUtils;

namespace TransactionDumpFileComparer
{
	public sealed class ShardInformationContext
	{
		private Dictionary<string, TransactionInfo> _searchTable = new();

		private Dictionary<string, int> _transactionTypes = new();

		public static ShardInformationContext LoadFromFile(string fileName)
		{
			using TransactionFileReader r = new TransactionFileReader(fileName);

			ShardInformationContext ret = new ShardInformationContext();

			while (!r.Eof)
			{
				(string, byte[], Dictionary<string, string>) record = default;
				try
				{
					record = r.ReadRecord();
					if (record.Item2 is null)
					{
						continue;
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Unexpected Eof={0}", r.Eof);
					continue;
				}

				var tx = WavesEnterprise.Transaction.Parser.ParseFrom(record.Item2);

				//var jsonString = formatter.Format(tx);
				if (record.Item3 == null)
				{

				}
				else
				{
					var key = GetTransactionSearchKey(tx);

					var type = tx.GetOperationType();
					if (!ret._transactionTypes.TryGetValue(type, out var oldValue))
					{
						ret._transactionTypes.Add(type, 1);
					}
					else
					{
						ret._transactionTypes[type] = oldValue + 1;
					}

					ret._searchTable.TryAdd(key, new TransactionInfo()
					{
						Key = key,
						Transaction = record.Item2
					});
				}
			}

			return ret;
		}

		public static string GetTransactionSearchKey(WavesEnterprise.Transaction tx)
		{
			var key = Base58.EncodePlain(tx.GetContractId())
						+ ":" + Base58.EncodePlain(tx.GetTransactionId())
						+ ":" + Base58.EncodePlain(tx.GetNestedTxId());
			return key;
		}


		public IReadOnlyDictionary<string,int> TransactionTypes
		{
			get { return _transactionTypes; }
		}

		public int TransactionCount
		{
			get { return _searchTable.Count; }
		}



		public static ShardInformationCompareResult Compare(ShardInformationContext left,ShardInformationContext right)
		{
			HashSet<string> processed = new();

			ShardInformationCompareResult ret = new();

			foreach (var leftPair in left._searchTable)
			{
				if (processed.Contains(leftPair.Value.Key))
				{
					continue;
				}

				if (right._searchTable.TryGetValue(leftPair.Key,out var info))
				{
					if (info.Transaction.SequenceEqual(leftPair.Value.Transaction))
					{
						ret.Same++;
					}
					else
					{
						ret.NotSame++;
					}
				}
				else
				{
					ret.Unique1++;
				}
				processed.Add(leftPair.Key);
			}

			foreach(var rightPair in right._searchTable)
			{
				if (processed.Contains(rightPair.Value.Key))
				{
					continue;
				}

				ret.Unique2++;
			}

			return ret;
		}
	}

	public sealed class ShardInformationCompareResult
	{
		public int Unique1 { get; set; }
		public int Unique2 { get; set; }
		public int Same { get; set; }
		public int NotSame { get; set; }
	}
}
