using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using TinyCsvParser;
using TinyCsvParser.Mapping;

namespace Voting2021.FilesUtils
{
	public class CsvFileReader
	{
		private readonly CsvParserOptions _csvParserOptions;
		private readonly CsvParser<Record> _csvParser;


		public CsvFileReader()
		{
			_csvParserOptions = new CsvParserOptions(false, ';');
			_csvParser = new CsvParser<Record>(_csvParserOptions, new RecordMap());
		}

		public Result ReadFromStream(Stream s)
		{
			var query = _csvParser.ReadFromStream(s, Encoding.UTF8);
			return ProcessData(query);
		}

		public Result ReadFromCsvFile(string fileName)
		{
			var query = _csvParser.ReadFromFile(fileName, Encoding.UTF8);
			return ProcessData(query);
		}

		public Result ReadStrictOneFromZipFileStream(Stream zipFileStream)
		{
			using var archive = SharpCompress.Archives.Zip.ZipArchive.Open(zipFileStream);
			var entry = archive.Entries.Where(x => x.Key?.EndsWith("csv") == true).SingleOrDefault();
			if (entry is null)
			{
				throw new InvalidOperationException();
			}
			using var csvFileStream = entry.OpenEntryStream();
			return ReadFromStream(csvFileStream);
		}

		public Result ReadStrictOneFromZipFile(string fileName)
		{
			var data = File.ReadAllBytes(fileName);
			using var memoryStream = new MemoryStream(data, false);
			return ReadStrictOneFromZipFileStream(memoryStream);
		}

		private Result ProcessData(System.Linq.ParallelQuery<CsvMappingResult<Record>> query)
		{
			List<Record> ret = new();
			List<string> unprocessed = new();
			foreach (var item in query)
			{
				if (item.IsValid)
				{
					ret.Add(item.Result);
				}
				else
				{
					unprocessed.Add(item.Error.UnmappedRow);
				}
			}
			return new Result(ret.ToArray(), unprocessed.ToArray());
		}

		public sealed class Result
		{
			private Record[] _records;
			private string[] _unprocessedRows;

			public Result(Record[] records, string[] unprocessedRows)
			{
				_records = records;
				_unprocessedRows = unprocessedRows;
			}

			public Record[] Records
			{
				get { return _records; }
			}

			public string[] UnprocessedRows
			{
				get { return _unprocessedRows; }
			}
		}

		public sealed class Record
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
		}

		private sealed class RecordMap
			: CsvMapping<Record>
		{
			public RecordMap()
			{
				MapProperty(0, x => x.NestedTxId);
				MapProperty(1, x => x.Type);
				MapProperty(2, x => x.Signature);
				MapProperty(3, x => x.Version);
				MapProperty(4, x => x.Timestamp);
				MapProperty(5, x => x.SenderPublicKey);
				MapProperty(6, x => x.Fee);
				MapProperty(7, x => x.FeeAssetId);
				MapProperty(8, x => x.Params);
				MapProperty(9, x => x.Diff);
				MapProperty(10, x => x.Extra);
				MapProperty(11, x => x.Rollback);
			}
		}
	}
}
