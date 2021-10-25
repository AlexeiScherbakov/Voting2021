using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Voting2021.BlockchainWatcher.Services;
using Voting2021.Database;
using Voting2021.Database.Data;

namespace Voting2021.BlockchainWatcher.Web.Services
{

	public class TransactionsCopyService
	{
		long? _progressCurrent;
		long? _progressTotal;

		private readonly ILogger<TransactionsCopyService> _logger;
		private readonly ITransactionStore _transactionStore;
		private readonly StateStore _stateStore;

		private ManualResetEventSlim _copyFileEvent = new ManualResetEventSlim();

		public TransactionsCopyService(ILogger<TransactionsCopyService> logger, ITransactionStore transactionStore, IBlockchainEventProcessor eventProcessor)
		{
			_logger = logger;
			_transactionStore = transactionStore;
			_stateStore = eventProcessor.StateStore;
		}


		public (bool,long,long) CopyFileOperation(string fileName,long startBlock,long endBlock)
		{
			if (_progressCurrent.HasValue)
			{
				return (true, _progressCurrent.Value, _progressTotal.Value);
			}

			using var session = _stateStore.OpenSession();
			session.DefaultReadOnly = true;

			long start = 0;
			if (startBlock != 0)
			{
				var startBlockEntity = session.Query<Block>()
					.Where(x => x.Height <= startBlock)
					.OrderByDescending(x => x.Height)
					.First();

				if (startBlockEntity != null)
				{
					start = startBlockEntity.StartOffset;
				}
			}
			long end = 0;
			if (endBlock != 0)
			{
				var endBlockEntity = session.Query<Block>()
					.Where(x => x.Height <= endBlock)
					.OrderByDescending(x => x.Height)
					.First();

				if (endBlockEntity != null)
				{
					end = endBlockEntity.EndOffset;
				}
			}

			if (end < start)
			{
				throw new InvalidOperationException();
			}

			Task.Factory.StartNew(() => CopyToWithProgressAsync(fileName, start, end), TaskCreationOptions.LongRunning);

			return new(false, 0, 0);
		}


		private async Task CopyToWithProgressAsync(string fileName,long start,long end)
		{
			try
			{
				await _transactionStore.CopyTo(fileName, start, end, new Progress<(long, long)>(progress =>
				{
					_progressCurrent = progress.Item1;
					_progressTotal = progress.Item2;
				}));
				_progressCurrent = null;
				_progressTotal = null;
			}
			catch(Exception e)
			{
				_logger.LogError(e, "Error in copy");
			}
			_progressCurrent = null;
			_progressTotal = null;
		}

		public async Task CopyToAsync(string fileName,long startBlock,long endBlock)
		{
			using var session = _stateStore.OpenSession();
			session.DefaultReadOnly = true;

			long start = 0;
			if (startBlock != 0)
			{
				var startBlockEntity = session.Query<Block>()
					.Where(x => x.Height <= startBlock)
					.OrderByDescending(x => x.Height)
					.First();

				if (startBlockEntity != null)
				{
					start = startBlockEntity.StartOffset;
				}
			}
			long end = 0;
			if (endBlock != 0)
			{
				var endBlockEntity = session.Query<Block>()
					.Where(x => x.Height <= endBlock)
					.OrderByDescending(x => x.Height)
					.First();

				if (endBlockEntity != null)
				{
					end = endBlockEntity.EndOffset;
				}
			}

			await _transactionStore.CopyTo(fileName, start, end, new Progress<(long, long)>(progress =>
			{
			}));
		}
	}
}
