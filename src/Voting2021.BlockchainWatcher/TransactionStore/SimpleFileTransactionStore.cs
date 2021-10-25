using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Voting2021.BlockchainWatcher.Settings;

namespace Voting2021.BlockchainWatcher.Services
{
	public sealed class SimpleFileTransactionStore
		: ITransactionStore
	{
		private FileStream _file;
		private TransactionFormatter _transactionFormatter;
		private long _currentOffset = 0;

		private string _transactionLogFileName;
		public SimpleFileTransactionStore(TransactionFormatter transactionFormatter,
			IOptions<TransactionStoreSettings> options)
		{
			_transactionFormatter = transactionFormatter;
			_transactionLogFileName = options.Value.TransactionLogFile;
			_file = new FileStream(_transactionLogFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 256 * 1024, FileOptions.WriteThrough);
			if (_file.Length > 0)
			{
				_file.Position = _file.Length;
				_currentOffset = _file.Position;
			}
		}

		public void Dispose()
		{
			_file.Flush();
			_file.Close();
			_file.Dispose();
		}

		public long GetCurrentOffset()
		{
			return _currentOffset;
		}

		public void AddEntry(string key, byte[] value, Dictionary<string, string> headers)
		{
			var array = _transactionFormatter.WriteTransaction(key, value, headers);
			_file.Write(array);
			_file.Flush();
			_currentOffset = _file.Position;
		}


		public async Task CopyTo(string fileName,long start,long end,IProgress<(long,long)> progressUpdater=null)
		{
			using var src = new FileStream(_transactionLogFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 256 * 1024, FileOptions.SequentialScan);
			src.Position = start;

			if (end > src.Length)
			{
				end = src.Length;
			}

			using var dst= new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 256 * 1024, FileOptions.WriteThrough);

			Memory<byte> buffer = new byte[10 * 1024 * 1024];
			long total = end - start;
			long count = end - start;
			long current = 0;
			while (count > 0)
			{
				var countForRead = (int)Math.Min(count, buffer.Length);
				int readedCount = await src.ReadAsync(buffer.Slice(0, countForRead));
				await dst.WriteAsync(buffer.Slice(0, readedCount));
				count -= readedCount;
				current += readedCount;
				if (progressUpdater != null)
				{
					progressUpdater.Report((current, total));
				}
			}
			dst.Flush();
		}
	}
}
