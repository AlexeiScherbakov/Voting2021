using System;
using System.Collections.Generic;
using System.IO;

using Voting2021.FilesUtils;

using VotingFilesDownloader.Database;

namespace StatDownloadVerifier
{
	internal static class Program
	{
		static void Main(string[] args)
		{
			// внешний SSD, временная директория для sqlite
			//System.Environment.SetEnvironmentVariable("TEMP", "E:\\temp", EnvironmentVariableTarget.Process);

			System.Threading.ThreadPool.GetMinThreads(out var workerThreads, out var completitionThreads);
			System.Threading.ThreadPool.SetMinThreads(Math.Max(workerThreads, 16), Math.Max(completitionThreads, 16));


			string directory = @"C:\Projects\Voting2021\src\VotingFilesDownloader\bin\Debug\net6.0\data\files\";
			//ValidateFilesInDirectory(directory);
			PostprocessTransactions(directory);
			Console.ReadLine();
		}


		private static IEnumerable<string> GetAllTransactionFiles(string directory)
		{
			var contractDirs = Directory.GetDirectories(directory);
			foreach (var contractDir in contractDirs)
			{
				var contractFiles = Directory.GetFiles(contractDir, "*.zip");
				foreach (var contractFile in contractFiles)
				{
					yield return contractFile;
				}
			}
		}


		private static void PostprocessTransactions(string directory)
		{
			var dbFileName = @"C:\Projects\Voting2021\src\VotingFilesDownloader\bin\Debug\net6.0\data\votings.db3";
			System.Data.SQLite.SQLiteConnectionStringBuilder b = new();
			b.BinaryGUID = true;
			b.ForeignKeys = true;
			b.DataSource = dbFileName;
			var connectionString = b.ToString();
			bool create = !File.Exists(dbFileName);

			var filesDb = VotingFilesDatabase.Sqlite(connectionString, create ? VotingFilesDatabase.Mode.Create : VotingFilesDatabase.Mode.Update);
			TransactionLoader transactionLoader = new TransactionLoader(filesDb);
			ExecuteLoad(transactionLoader, directory);
		}

		private static void ValidateFilesInDirectory(string directory)
		{
			SimpleFileVerifier verifier = new SimpleFileVerifier();
			ExecuteLoad(verifier, directory);
			Console.WriteLine("Verifier Tx={0} {1}/{2}", verifier.TotalTransactions, verifier.FilesWithInvalidRecords.Count, verifier.TotalFiles);
			Console.ReadLine();
		}


		private static void ExecuteLoad<TLoader>(TLoader loader,string directory)
			where TLoader:BaseFileLoader
		{
			foreach (var file in GetAllTransactionFiles(directory))
			{
				loader.AddFile(file);
			}
			loader.WaitUntilCompletedAsync().GetAwaiter().GetResult();
		}
	}
}
