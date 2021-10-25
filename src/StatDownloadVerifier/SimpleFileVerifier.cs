using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Voting2021.FilesUtils;

namespace StatDownloadVerifier
{



	public sealed class SimpleFileVerifier
		: BaseFileLoader<CsvFileReader.Result>
	{
		private CsvFileReader _reader = new CsvFileReader();


		private List<string> _filesWithInvalidRecords = new List<string>();

		private int _totalFiles = 0;
		private int _totalTransactions = 0;

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
			foreach (var data in processedFileData)
			{
				if (data.Item2.UnprocessedRows.Length > 0)
				{
					_filesWithInvalidRecords.Add(data.Item1);
				}
				_totalTransactions += data.Item2.Records.Length;
				_totalFiles++;
			}
		}
	}
}
