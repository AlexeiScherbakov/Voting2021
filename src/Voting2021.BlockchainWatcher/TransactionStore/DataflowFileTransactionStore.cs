using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.Options;

using Voting2021.BlockchainWatcher.Settings;

namespace Voting2021.BlockchainWatcher.Services
{
	public sealed class DataflowFileTransactionStore
		: ITransactionStore
	{
		private BufferBlock<TransactionToStore> _bufferBlock;
		private TransformBlock<TransactionToStore, byte[]> _transformBlock;
		private BatchBlock<byte[]> _batchBlock;
		private ActionBlock<byte[][]> _writeBlock;

		private byte[] _writeBuffer;

		private FileStream _file;


		private readonly TransactionFormatter _transactionFormatter;

		private long _currentOffset = 0;
		private readonly string _transactionLogFileName;
		public DataflowFileTransactionStore(
			Microsoft.Extensions.Hosting.IHostApplicationLifetime hostApplicationLifetime,
			TransactionFormatter transactionFormatter,
			IOptions<TransactionStoreSettings> options)
		{
			_transactionLogFileName = options.Value.TransactionLogFile;
			_transactionFormatter = transactionFormatter;

			_file = new FileStream(_transactionLogFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 256 * 1024, FileOptions.WriteThrough);
			if (_file.Length > 0)
			{
				_file.Position = _file.Length;
				_currentOffset = _file.Position;
			}
			_writeBuffer = new byte[10 * 1024 * 1024];

			_bufferBlock = new BufferBlock<TransactionToStore>(new DataflowBlockOptions()
			{
				EnsureOrdered = true
			});
			_transformBlock = new TransformBlock<TransactionToStore, byte[]>(CreateMessageContainer, new ExecutionDataflowBlockOptions()
			{
				EnsureOrdered = true,
				MaxDegreeOfParallelism = 8
			});
			_batchBlock = new BatchBlock<byte[]>(20, new GroupingDataflowBlockOptions()
			{
				EnsureOrdered = true
			});
			_writeBlock = new ActionBlock<byte[][]>(WriteTransactionsToFile, new ExecutionDataflowBlockOptions()
			{
				MaxDegreeOfParallelism = 1,
				EnsureOrdered = true
			});


			_bufferBlock.LinkTo(_transformBlock, new DataflowLinkOptions()
			{

			});
			_transformBlock.LinkTo(_batchBlock, new DataflowLinkOptions()
			{

			});
			_batchBlock.LinkTo(_writeBlock, new DataflowLinkOptions()
			{

			});
			hostApplicationLifetime.ApplicationStopped.Register(ApplicationStopped);
			
		}

		public long CurrentOffset
		{
			get { return _currentOffset; }
		}

		public long GetCurrentOffset()
		{
			return _currentOffset;
		}

		private void ApplicationStopped()
		{
			WaitUntilStopped().Wait();
		}

		private async Task WaitUntilStopped()
		{
			_bufferBlock.Complete();
			await _bufferBlock.Completion;
			_transformBlock.Complete();
			await _transformBlock.Completion;
			_batchBlock.Complete();
			await _batchBlock.Completion;
			_writeBlock.Complete();
			await _writeBlock.Completion;
		}

		public void AddEntry(string key, byte[] value, Dictionary<string, string> headers)
		{
			_bufferBlock.Post(new TransactionToStore()
			{
				Key = key,
				Value = value,
				Headers = headers
			});
		}


		private byte[] CreateMessageContainer(TransactionToStore transactionToStore)
		{
			return _transactionFormatter.WriteTransaction(transactionToStore.Key, transactionToStore.Value, transactionToStore.Headers);
		}

		private void WriteTransactionsToFile(byte[][] messages)
		{
			int totalSize = messages.Select(x => x.Length).Sum();
			byte[] buffer = null;
			// Мы так подберем параметры так чтобы это условие в 99.(9)% случаях выполнялось
			if (totalSize < _writeBuffer.Length)
			{
				buffer = _writeBuffer;
			}
			else
			{
				buffer = new byte[totalSize];
			}
			Span<byte> dst = buffer;
			for (int i = 0; i < messages.Length; i++)
			{
				messages[i].CopyTo(dst);
				dst = dst.Slice(messages[i].Length);
			}

			_file.Write(buffer.AsSpan().Slice(0, totalSize));
			_file.Flush();
			_currentOffset = _file.Position;
		}

		public async Task CopyTo(string fileName, long start, long end, IProgress<(long, long)> progressUpdater = null)
		{
			using var src = new FileStream(_transactionLogFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 256 * 1024, FileOptions.SequentialScan);
			src.Position = start;

			using var dst = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 256 * 1024, FileOptions.WriteThrough);

			Memory<byte> buffer = new byte[10 * 1024 * 1024];
			long total = end - start;
			long count = end - start;
			long current = 0;
			while (count > 0)
			{
				int readedCount = await src.ReadAsync(buffer);
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

		private sealed class TransactionToStore
		{
			public string Key;
			public byte[] Value;
			public Dictionary<string, string> Headers;
		}
	}
}
