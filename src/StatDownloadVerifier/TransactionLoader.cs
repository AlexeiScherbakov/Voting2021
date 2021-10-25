using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NHibernate.Linq;

using Voting2021.FilesUtils;

using VotingFilesDownloader.Database;

namespace StatDownloadVerifier
{
	public sealed class TransactionLoader
		: BaseFileLoader<CsvFileReader.Result>
	{
		private CsvFileReader _reader = new CsvFileReader();


		private List<string> _filesWithInvalidRecords = new List<string>();

		private int _totalFiles = 0;
		private int _totalTransactions = 0;

		private VotingFilesDatabase _votingFileDatabase;

		public TransactionLoader(VotingFilesDatabase votingFileDatabase)
		{
			_votingFileDatabase = votingFileDatabase;
		}

		public List<string> FilesWithInvalidRecords
		{
			get { return _filesWithInvalidRecords; }
		}

		public int TotalFiles
		{
			get { return _totalFiles; }
		}

		public int TotalTransactions
		{
			get { return _totalTransactions; }
		}

		protected override void OnFileLoading(string fileName)
		{
			Console.WriteLine("Reading file {0}", fileName);
		}

		protected override (string, CsvFileReader.Result) ProcessFileBlock((string, byte[]) fileData)
		{
			var memoryStream = new MemoryStream(fileData.Item2, false);
			return (fileData.Item1, _reader.ReadStrictOneFromZipFileStream(memoryStream));
		}

		protected override void AccumulateBlock((string, CsvFileReader.Result)[] processedFileData)
		{
			var normalizedFileData = processedFileData.Select(x => (Path.GetFileName(x.Item1), x.Item2)).ToArray();

			using var session = _votingFileDatabase.OpenSession();
			using var tr = session.BeginTransaction();

			var files = normalizedFileData.Select(x => x.Item1).Distinct().ToArray();

			var searchDictionaryForFiles = session.Query<DbVotingFile>()
				.Where(x => files.Contains(x.FileName))
				.Fetch(x => x.Transactions)
				.ToDictionary(x => x.FileName, x => x);

			// поисковые словари
			Dictionary<string, Dictionary<string, DbTransactionInFile>> searchDictionaryForTransactions = new();
			foreach(var pair in searchDictionaryForFiles)
			{
				Dictionary<string, DbTransactionInFile> fileTransactionSearch = null;
				if (!searchDictionaryForTransactions.TryGetValue(pair.Key,out fileTransactionSearch))
				{
					fileTransactionSearch = new Dictionary<string, DbTransactionInFile>();
					searchDictionaryForTransactions.Add(pair.Key, fileTransactionSearch);
				}
				foreach(var tx in pair.Value.Transactions)
				{
					fileTransactionSearch.Add(tx.NestedTxId, tx);
				}
			}

			foreach (var data in normalizedFileData)
			{
				if (data.Item2.UnprocessedRows.Length > 0)
				{
					_filesWithInvalidRecords.Add(data.Item1);
				}
				_totalTransactions += data.Item2.Records.Length;
				_totalFiles++;
				if(searchDictionaryForFiles.TryGetValue(data.Item1,out var dbVotingFile))
				{
					// нашли файл
					var fileTransactionSearch = searchDictionaryForTransactions[data.Item1];
					foreach(var transactionInFile in data.Item2.Records)
					{
						if (!fileTransactionSearch.TryGetValue(transactionInFile.NestedTxId,out var dbTx))
						{
							dbTx = new DbTransactionInFile()
							{
								File = dbVotingFile,
								NestedTxId= transactionInFile.NestedTxId,
								Type=transactionInFile.Type,
								Signature=transactionInFile.Signature,
								Version=transactionInFile.Version,
								Timestamp=transactionInFile.Timestamp,
								SenderPublicKey=transactionInFile.SenderPublicKey,
								Fee=transactionInFile.Fee,
								FeeAssetId=transactionInFile.FeeAssetId,
								Params=transactionInFile.Params,
								Diff=transactionInFile.Diff,
								Extra=transactionInFile.Extra,
								Rollback=transactionInFile.Rollback
							};
							session.Save(dbTx);
							fileTransactionSearch.Add(transactionInFile.NestedTxId, dbTx);
						}
					}
				}
			}
			tr.Commit();
		}
	}
}
