using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

using Google.Protobuf;

using NHibernate.Linq;

using Voting2021.BlockchainClient;
using Voting2021.BlockchainWatcher.ResearchDatabase;
using Voting2021.Database;
using Voting2021.FilesUtils;

namespace BlockchainVerifier
{
	class Program
	{
		static void Main(string[] args)
		{
			// внешний SSD, временная директория для sqlite
			System.Environment.SetEnvironmentVariable("TEMP", "E:\\temp", EnvironmentVariableTarget.Process);

			string basePath = @"e:\blockchain_data\blockchain_dump_3dayend\";


			var dbFileName = Path.Combine(basePath, "research.db3");
			System.Data.SQLite.SQLiteConnectionStringBuilder b = new();
			b.BinaryGUID = true;
			b.ForeignKeys = true;
			b.DataSource = dbFileName;
			b.DefaultTimeout = 10 * 60;
			b.JournalMode = SQLiteJournalModeEnum.Wal;
			var connectionString = b.ToString();
			var create = !File.Exists(dbFileName);

			var researchDatabase=ResearchDatabase.Sqlite(connectionString, create);

			BlockchainDataImporter importer = new BlockchainDataImporter(researchDatabase);

			int transactionCount = 0;
			HashSet<string> set = new HashSet<string>();

			for (int i = 1; i <= 4; i++)
			{
				var shardDirectory = Path.Combine(basePath, i.ToString());
				var dbFile = Path.Combine(shardDirectory, "blockchain.db3");

				Console.WriteLine("Processing shard {0} state db {1}", i, dbFile);
				string shardStateDatabaseConnectionString = null;
				{
					b = new();
					b.BinaryGUID = true;
					b.ForeignKeys = false;
					b.JournalMode = System.Data.SQLite.SQLiteJournalModeEnum.Wal;
					b.DataSource = dbFile;
					shardStateDatabaseConnectionString = b.ToString();
				}
				using var shardState = StateStore.Sqlite(shardStateDatabaseConnectionString, false);
				using (var srcConnection = shardState.OpenSession())
				{
					var allBlocks = srcConnection.Query<Voting2021.Database.Data.Block>()
						.ToArray();
					using (var session = researchDatabase.OpenSession())
					using (var tr = session.BeginTransaction())
					{
						foreach (var block in allBlocks)
						{
							DbBlockchainBlock dbBlock = new DbBlockchainBlock()
							{
								Height = block.Height,
								Shard = i,
								Signature = Base58.EncodePlain(block.Signature),
								Timestamp = block.Timestamp
							};
							session.Save(dbBlock);
						}
						tr.Commit();
					}
				}
			}

			for (int i = 1; i <= 4; i++)
			{
				Console.WriteLine("Begin processing shard {0} data", i);
				var shardDirectory = Path.Combine(basePath, i.ToString());
				var transactionFile = Path.Combine(shardDirectory, "transaction_output.bin");
						
				Console.WriteLine("Processing shard {0} transactionFile {1}",i, transactionFile);
				using TransactionFileReader r = new TransactionFileReader(transactionFile);
				
				while (!r.Eof)
				{
					var record = r.ReadRecord();
					if (record.Item2 is null)
					{
						continue;
					}
					var tx = WavesEnterprise.Transaction.Parser.ParseFrom(record.Item2);

					//var jsonString = formatter.Format(tx);
					if (record.Item3 == null)
					{

					}
					else
					{
						importer.AddTransaction(i, tx, record.Item3);
						if (record.Item3.TryGetValue("id", out var txId))
						{
							set.Add(txId);
						}
						transactionCount++;
					}
				}
			}

			Console.WriteLine("Total transactions {0}, unique transaction {1}", transactionCount, set.Count);
			Console.WriteLine("Waiting for import completing");
			importer.WaitUntilCompletedAsync().GetAwaiter().GetResult();
			Console.WriteLine("Import completed");
			Console.ReadLine();
		}
	}
}
