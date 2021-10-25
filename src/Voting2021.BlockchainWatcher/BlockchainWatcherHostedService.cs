using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Voting2021.BlockchainClient;
using Voting2021.BlockchainClient.ObjectModel;
using Voting2021.BlockchainWatcher.Settings;

namespace Voting2021.BlockchainWatcher.Services
{
	public sealed class BlockchainWatcherHostedService
		: IHostedService
	{
		private readonly ILogger<BlockchainWatcherHostedService> _logger;
		private BlockchainConnectionSettings _blockchainConnectionSettings;


		private IBlockchainEventProcessor _blockchainEventProcessor;
		private Task _blockchainReaderTask;

		private CancellationTokenSource _cts = new CancellationTokenSource();

		private ManualResetEventSlim _mre = new ManualResetEventSlim(false);

		private long _currentHeight;
		private long _totalTransactions;
		public BlockchainWatcherHostedService(
			ILogger<BlockchainWatcherHostedService> logger,
			IOptions<BlockchainConnectionSettings> options,
			IBlockchainEventProcessor blockchainEventProcessor)
		{
			_logger = logger;
			_blockchainConnectionSettings = options.Value;
			_blockchainEventProcessor = blockchainEventProcessor;
		}


		public long CurrentHeight
		{
			get { return _currentHeight; }
		}


		public Task StartAsync(CancellationToken cancellationToken)
		{
			_blockchainReaderTask = Task.Factory.StartNew(ReceiveCycle, TaskCreationOptions.LongRunning);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_cts.Cancel();
			_mre.Wait();
			return Task.CompletedTask;
		}

		private async Task ReceiveCycle()
		{
			while (!_cts.IsCancellationRequested)
			{
				try
				{
					await BlockchainReader();
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Error in receive cycle", e.Message);
				}
			}
			_mre.Set();
		}

		private async Task BlockchainReader()
		{
			var lastBlock = _blockchainEventProcessor.GetLastProcessedBlockInfo();
			if (lastBlock.signature is null)
			{
				_logger.LogInformation("Starting from Genesis Block");
			}
			else
			{
				_logger.LogInformation("Starting from {BlockSigHex}/{BlockSig58},{BlockHeight}",
					Convert.ToHexString(lastBlock.signature),
					Base58.EncodePlain(lastBlock.signature),
					lastBlock.height);
			}

			using var reader = new EventStreamReader(_blockchainConnectionSettings.ConnectionUrl, lastBlock.signature);
			var metadata = await reader.Stream.ResponseHeadersAsync;
			while (await reader.Stream.ResponseStream.MoveNext(_cts.Token))
			{
				var evnt = reader.Stream.ResponseStream.Current;
				if (evnt.BlockAppended is not null)
				{
					_blockchainEventProcessor.ProcessBlockAppended(evnt.BlockAppended);
					_currentHeight = evnt.BlockAppended.Height;
					_logger.LogDebug("Block appended {BlockHeight} Transactions={TransactionCount}",
						_currentHeight,
						evnt.BlockAppended.TxIds.Count);

					_totalTransactions += evnt.BlockAppended.TxIds.Count;
				}
				else if (evnt.AppendedBlockHistory is not null)
				{
					_blockchainEventProcessor.ProcessAppendedBlockHistory(evnt.AppendedBlockHistory);
					_currentHeight = evnt.AppendedBlockHistory.Height;
					_logger.LogDebug("Appended block history {BlockHeight} Transactions={TransactionCount}",
						_currentHeight,
						evnt.AppendedBlockHistory.Txs.Count);

					_totalTransactions += evnt.AppendedBlockHistory.Txs.Count;
				}
				else if (evnt.MicroBlockAppended is not null)
				{
					_logger.LogDebug("Microblock Transactions={TransactionCount}",
						   evnt.MicroBlockAppended.Txs.Count);
					_blockchainEventProcessor.ProcessMicroBlockAppended(evnt.MicroBlockAppended);
				}
				else if (evnt.RollbackCompleted is not null)
				{
					_blockchainEventProcessor.ProcessRollbackCompleted(evnt.RollbackCompleted);
				}
			}
		}
	}
}
