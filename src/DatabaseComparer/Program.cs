using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Google.Protobuf;
using Google.Protobuf.Collections;

using Voting2021.BlockchainClient;
using Voting2021.BlockchainWatcher.ResearchDatabase;

using VotingFilesDownloader.Database;

namespace DatabaseComparer
{
	class Program
	{
		static JsonFormatter ProtobuffJsonFormatter = new JsonFormatter(new JsonFormatter.Settings(false));
		static int Main(string[] args)
		{
			// внешний SSD, временная директория для sqlite
			//System.Environment.SetEnvironmentVariable("TEMP", "E:\\temp", EnvironmentVariableTarget.Process);

			const string votingDatabaseFile = @"e:\votings\votings.db3";
			//const string researchDatabaseFile = @"e:\blockchain_data\blockchain_dump_final\research.db3";
			const string researchDatabaseFile = @"e:\votings\research.db3";
			
			VotingFilesDatabase votingFilesDatabase = null;
			{
				System.Data.SQLite.SQLiteConnectionStringBuilder b = new();
				b.BinaryGUID = true;
				b.ForeignKeys = true;
				b.DataSource = votingDatabaseFile;
				var connectionString = b.ToString();
				votingFilesDatabase = VotingFilesDatabase.Sqlite(connectionString, VotingFilesDatabase.Mode.ReadOnly);
			}
			ResearchDatabase researchDatabase = null;
			{
				System.Data.SQLite.SQLiteConnectionStringBuilder b = new();
				b.BinaryGUID = true;
				b.ForeignKeys = true;
				b.DataSource = researchDatabaseFile;
				var connectionString = b.ToString();
				researchDatabase = ResearchDatabase.Sqlite(connectionString, false);
			}
			int err = CompareDatabases(votingFilesDatabase, researchDatabase);
			if (err!=0)
			{
				Console.WriteLine("Error");
			}
			else
			{
				Console.WriteLine("OK!");
			}
			return err;
		}

		static int CompareDatabases(VotingFilesDatabase votingFilesDatabase, ResearchDatabase researchDatabase)
		{
			if (!StatCompare(votingFilesDatabase, researchDatabase))
			{
				Console.Error.WriteLine("Transaction count mismatch");
				return -1;
			}

			return TransactionsCompare(votingFilesDatabase, researchDatabase);
		}

		static bool StatCompare(VotingFilesDatabase votingFilesDatabase, ResearchDatabase researchDatabase)
		{
			using var session1 = votingFilesDatabase.OpenSession();
			var trCount1 = session1.Query<DbTransactionInFile>().Count();

			using var session2 = researchDatabase.OpenSession();
			var trCount2 = session2.Query<DbBlockchainTransaction>().Count();

			Console.WriteLine("Total transactions in files {0}", trCount1);
			Console.WriteLine("Total transactions in dump {0}", trCount2);

			return trCount1 == trCount2;
		}

		static int TransactionsCompare(VotingFilesDatabase votingFilesDatabase, ResearchDatabase researchDatabase)
		{
			using var session1 = votingFilesDatabase.OpenSession();
			var startTimestamp = session1.Query<DbTransactionInFile>().Select(x => x.Timestamp).Min();
			var endTimestamp = session1.Query<DbTransactionInFile>().Select(x => x.Timestamp).Max();

			var duration = endTimestamp - startTimestamp;
			// У нас где-то 20Гб данных, поэтому поделим весь промежуток времени на 200 частей, чтоб в среднем все гарантировано влезло
			var timePart = duration / 200;

			int processedTransactions = 0;
			int pos = 0;
			for (long i = startTimestamp; i < endTimestamp + timePart; i += timePart)
			{
				var context = LoadCompareContext(votingFilesDatabase, researchDatabase, i, timePart);
				if (!CompareTransactions(context))
				{
					Console.Error.WriteLine("transactions are not equals");
					return -1;
				}
				processedTransactions += context.BlockchainTransactions.Length;
				pos += 1;
				Console.WriteLine("{0}: Processed transactions: {1}", pos, processedTransactions);

			}
			return 0;
		}

		static CompareContext LoadCompareContext(VotingFilesDatabase votingFilesDatabase, ResearchDatabase researchDatabase, long startTimestamp, long duration)
		{
			using var session1 = votingFilesDatabase.OpenSession();
			var endTimestamp = startTimestamp + duration;
			var tr1 = session1.Query<DbTransactionInFile>()
				.Where(x => (x.Timestamp >= startTimestamp) && (x.Timestamp <= endTimestamp))
				.Select(x => new CompareContext.TransactionInFile()
				{
					ContractId = x.File.Voting.ContractId,
					Diff = x.Diff,
					Extra = x.Extra,
					Fee = x.Fee,
					FeeAssetId = x.FeeAssetId,
					NestedTxId = x.NestedTxId,
					Params = x.Params,
					Rollback = x.Rollback,
					SenderPublicKey = x.SenderPublicKey,
					Signature = x.Signature,
					Timestamp = x.Timestamp,
					Type = x.Type,
					Version = x.Version
				})
				.ToArray();

			using var session2 = researchDatabase.OpenSession();
			var tr2 = session2.Query<DbBlockchainTransaction>()
				.Where(x => (x.NestedTimestamp >= startTimestamp) && (x.NestedTimestamp <= endTimestamp))
				.ToArray();

			return new CompareContext()
			{
				FilesTransactions = tr1,
				BlockchainTransactions = tr2
			};
		}



		private static bool CompareTransactions(CompareContext context)
		{
			context.BuildFileTransactionSearch();

			ConcurrentBag<DbBlockchainTransaction> anomalTransactions = new();

			var result=Parallel.ForEach(context.BlockchainTransactions, new ParallelOptions()
			{
				MaxDegreeOfParallelism = 8
			}, blockchainTransaction =>
			{
				if (!TransactionCompareIteration(blockchainTransaction, context))
				{
					anomalTransactions.Add(blockchainTransaction);
				}
			});
			if (!result.IsCompleted)
			{
				Console.WriteLine("Parallel loop is not completed");
				return false;
			}
			if (anomalTransactions.Count > 0)
			{
				Console.WriteLine("\t Anomal transactions count {0}", anomalTransactions.Count);
				return false;
			}

			return true;
		}

		private static bool TransactionCompareIteration(DbBlockchainTransaction blockchainTransaction, CompareContext context)
		{
			if (!context.FileTransactionSearch.TryGetValue(blockchainTransaction.NestedTimestamp, out var list))
			{
				Console.Error.WriteLine("Cannot find transaction with timestamp {0}", blockchainTransaction.NestedTimestamp);
				return false;
			}
			CompareContext.TransactionInFile list0;
			if (list.Count == 1)
			{
				if (!IsTransactionEquals(list[0], blockchainTransaction))
				{
					Console.Error.WriteLine("Transactions with timestamp {0} are not equals", blockchainTransaction.NestedTimestamp);
					return false;
				}
				return true;
			}
			else
			{
				// смотрим весь список если ни одна не подходит то ошибка
				bool found = false;
				foreach (var item in list)
				{
					found |= IsTransactionEquals(item, blockchainTransaction);
				}
				if (!found)
				{
					Console.Error.WriteLine("Transaction with timestamp {0} is not found in list", blockchainTransaction.NestedTimestamp);
					return false;
				}
				return true;
			}
		}
		private static bool IsTransactionEquals(CompareContext.TransactionInFile tx1, DbBlockchainTransaction tx2)
		{
			var tx = WavesEnterprise.Transaction.Parser.ParseFrom(tx2.TransactionBytes);
			string proof = null;
			string nestedTxId = Base58.EncodePlain(tx.GetNestedTxId());
			string contractId = Base58.EncodePlain(tx.GetContractId());

			string paramJson = null;
			string resultJson = null;
			RepeatedField<WavesEnterprise.DataEntry> parameters = null;
			RepeatedField<WavesEnterprise.DataEntry> results = null;
			if (tx.ExecutedContractTransaction is not null)
			{
				if (tx.ExecutedContractTransaction.Tx.CreateContractTransaction is not null)
				{
					proof = Base58.EncodePlain(tx.ExecutedContractTransaction.Tx.CreateContractTransaction.Proofs[0].ToByteArray());
					parameters = tx.ExecutedContractTransaction.Tx.CreateContractTransaction.Params;
					results = tx.ExecutedContractTransaction.Results;
				}
				else if (tx.ExecutedContractTransaction.Tx.CallContractTransaction is not null)
				{
					proof = Base58.EncodePlain(tx.ExecutedContractTransaction.Tx.CallContractTransaction.Proofs[0].ToByteArray());
					parameters = tx.ExecutedContractTransaction.Tx.CallContractTransaction.Params;
					results = tx.ExecutedContractTransaction.Results;
				}
			}

			if (tx1.Signature != proof)
			{
				return false;
			}
			if (tx1.NestedTxId != nestedTxId)
			{
				return false;
			}
			if (tx1.ContractId != contractId)
			{
				return false;
			}
			using var jsonParams=JsonDocument.Parse(tx1.Params);
			if (!CompareRepeatedFields(parameters, jsonParams))
			{
				return false;
			}
			using var jsonResults = JsonDocument.Parse(tx1.Diff);
			if (!CompareRepeatedFields(results, jsonResults))
			{
				return false;
			}
			return true;
		}

		private static bool CompareRepeatedFields(RepeatedField<WavesEnterprise.DataEntry> fields,JsonDocument json)
		{
			foreach (var dataEntry in fields)
			{
				bool found = false;
				foreach (var jsonEntry in json.RootElement.EnumerateArray())
				{
					var key = jsonEntry.GetProperty("key");
					var keyValue = key.GetString();
					if (keyValue == dataEntry.Key)
					{
						if (CompareDataEntry(dataEntry, jsonEntry))
						{
							found = true;
							break;
						}
					}
				}
				if (!found)
				{
					return false;
				}
			}
			return true;
		}
	
		private static bool CompareDataEntry(WavesEnterprise.DataEntry dataEntry, JsonElement json)
		{
			switch (dataEntry.ValueCase)
			{
				case WavesEnterprise.DataEntry.ValueOneofCase.StringValue:
					var str = json.GetProperty("stringValue").GetString();
					if (str != dataEntry.StringValue)
					{
						return false;
					}
					return true;
				case WavesEnterprise.DataEntry.ValueOneofCase.BinaryValue:
					var bytes = json.GetProperty("binaryValue").GetBytesFromBase64();
					var protobufBytes=dataEntry.BinaryValue.ToByteArray();
					if (!MemoryExtensions.SequenceEqual<byte>(bytes, protobufBytes))
					{
						return false;
					}
					return true;
				case WavesEnterprise.DataEntry.ValueOneofCase.IntValue:
					var intValue = json.GetProperty("intValue").GetInt64();
					if (intValue!=dataEntry.IntValue)
					{
						return false;
					}
					return true;
				default:
					return false;
			}
		}
	}


	public sealed class CompareContext
	{
		public TransactionInFile[] FilesTransactions;
		public DbBlockchainTransaction[] BlockchainTransactions;

		public Dictionary<long, List<TransactionInFile>> FileTransactionSearch = new();

		public void BuildFileTransactionSearch()
		{
			for (int i = 0; i < FilesTransactions.Length; i++)
			{
				var current = FilesTransactions[i];
				if (!FileTransactionSearch.TryGetValue(current.Timestamp,out var list))
				{
					list = new List<TransactionInFile>();
					FileTransactionSearch.Add(current.Timestamp, list);
				}
				list.Add(current);
			}
		}


		public sealed class TransactionInFile
		{
			public string NestedTxId { get; set; }

			public int Type { get; set; }

			public string Signature { get; set; }

			public string Version { get; set; }

			public long Timestamp { get; set; }

			public string SenderPublicKey { get; set; }

			public string Fee { get; set; }

			public string FeeAssetId { get; set; }

			public string Params { get; set; }

			public string Diff { get; set; }

			public string Extra { get; set; }

			public string Rollback { get; set; }

			public string ContractId { get; set; }
		}
	}
}
