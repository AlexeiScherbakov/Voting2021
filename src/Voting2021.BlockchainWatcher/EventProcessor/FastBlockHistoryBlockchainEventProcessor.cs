using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NHibernate.Linq;

using Voting2021.BlockchainClient;
using Voting2021.BlockchainClient.ObjectModel;
using Voting2021.BlockchainWatcher.Settings;
using Voting2021.Database;

namespace Voting2021.BlockchainWatcher.Services
{
	public sealed class FastBlockHistoryBlockchainEventProcessor
		: IBlockchainEventProcessor
	{
		private readonly ILogger<SequentialBlockchainEventProcessor> _logger;

		private ITransactionStore _transactionStore;
		private ITransactionCache _transactionCache;
		private Database.StateStore _stateStore;


		private Queue<WavesEnterprise.AppendedBlockHistory> _appendedBlockHistoryQueue = new Queue<WavesEnterprise.AppendedBlockHistory>();
		private int _totalAppendedBlockHistoryTransactions;


		public FastBlockHistoryBlockchainEventProcessor(
			ILogger<SequentialBlockchainEventProcessor> logger,
			ITransactionCache transactionCache,
			ITransactionStore transactionStore,
			IOptions<TransactionStoreSettings> options)
		{
			_logger = logger;
			_transactionStore = transactionStore;
			_transactionCache = transactionCache;
			System.Data.SQLite.SQLiteConnectionStringBuilder b = new();
			b.BinaryGUID = true;
			b.ForeignKeys = false;
			b.JournalMode = System.Data.SQLite.SQLiteJournalModeEnum.Wal;
			b.DataSource = options.Value.DatabaseFile;
			var connectionString = b.ToString();
			bool create = !File.Exists(options.Value.DatabaseFile);
			_stateStore = Database.StateStore.Sqlite(connectionString, create);
		}
		public StateStore StateStore
		{
			get { return _stateStore; }
		}

		public (byte[] signature, long height, long transactionCount) GetLastProcessedBlockInfo()
		{
			return _stateStore.GetLastBlockInfo();
		}

		public void ProcessBlockAppended(WavesEnterprise.BlockAppended blockAppended)
		{
			FlushAppendedBlockHistory();
			var transactionIds = blockAppended.TxIds?.Select(x => x.ToByteArray()).ToArray() ?? Array.Empty<byte[]>();
			var transactions = _transactionCache.GetTransactionsById(transactionIds);
			long startOffset = _transactionStore.GetCurrentOffset();
			_transactionStore.SendBlock(blockAppended, transactions);
			long endOffset = _transactionStore.GetCurrentOffset();
			SaveBlock(blockAppended, transactions, startOffset, endOffset);
		}

		public void ProcessAppendedBlockHistory(WavesEnterprise.AppendedBlockHistory appendedBlockHistory)
		{
			_appendedBlockHistoryQueue.Enqueue(appendedBlockHistory);
			_totalAppendedBlockHistoryTransactions += appendedBlockHistory.Txs.Count;
			if (_appendedBlockHistoryQueue.Count > 50 || _totalAppendedBlockHistoryTransactions > 10000)
			{
				FlushAppendedBlockHistory();
			}
		}

		private void FlushAppendedBlockHistory()
		{
			if (_appendedBlockHistoryQueue.Count == 0)
			{
				return;
			}
			using var session = _stateStore.OpenSession();
			using var tr = session.BeginTransaction();
			while (_appendedBlockHistoryQueue.Count > 0)
			{
				var appendedBlockHistory = _appendedBlockHistoryQueue.Dequeue();
				var transactions = appendedBlockHistory.Txs.Select(x => x).ToArray();

				long start = _transactionStore.GetCurrentOffset();

				_transactionStore.SendBlock(appendedBlockHistory, transactions);

				long end = _transactionStore.GetCurrentOffset();

				var dbBlock = new Database.Data.Block()
				{
					Height = appendedBlockHistory.Height,
					Signature = appendedBlockHistory.BlockSignature.ToByteArray(),
					Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(appendedBlockHistory.Timestamp).UtcDateTime,
					StartOffset = start,
					EndOffset = end
				};

				var dbTransactions = transactions.Select(x => new Database.Data.Tx()
				{
					Height = appendedBlockHistory.Height,
					ContractId = x.GetContractId(),
					TxId = x.GetTransactionId(),
					NestedTxId = x.GetNestedTxId(),
				});

				session.Save(dbBlock);
				foreach (var tx in dbTransactions)
				{
					session.Save(tx);
				}
			}
			tr.Commit();
			_totalAppendedBlockHistoryTransactions = 0;
		}

		public void ProcessMicroBlockAppended(WavesEnterprise.MicroBlockAppended microBlockAppended)
		{
			FlushAppendedBlockHistory();
			foreach (var tx in microBlockAppended.Txs)
			{
				var id = tx.GetTransactionId();
				_transactionCache.Put(id, tx);
			}
		}

		public void ProcessRollbackCompleted(WavesEnterprise.RollbackCompleted rollbackCompleted)
		{
			FlushAppendedBlockHistory();

			using var session = _stateStore.OpenSession();
			using var tr = session.BeginTransaction();
			var blockSig = rollbackCompleted.ReturnToBlockSignature.ToByteArray();

			var block = session.Query<Database.Data.Block>()
				.Where(x => x.Signature == blockSig)
				.SingleOrDefault();
			if (block is null)
			{
				_logger.LogWarning("Cannot find block with signature={BlockSignatureHex}/{BlockSignature58}", Convert.ToHexString(blockSig), Base58.EncodePlain(blockSig));
				return;
			}
			var height = block.Height;
			_logger.LogWarning("Rolling back to height={Height}", height);
			var transactions = session.Query<Database.Data.Tx>()
				.Where(x => x.Height > height)
				.ToArray();

			_transactionStore.SendRollback(transactions);

			int deletedBlocks = session.Query<Database.Data.Block>()
				.Where(x => x.Height > height)
				.Delete();

			_logger.LogInformation("Deleted {DeletedBlockCount} blocks from state store", deletedBlocks);

			int deletedTransactions = session.Query<Database.Data.Tx>()
				.Where(x => x.Height > height)
				.Delete();

			_logger.LogInformation("Deleted {DeletedBlockCount} transactions from state store", deletedTransactions);

			tr.Commit();
		}

		private void SaveBlock<TBlock>(TBlock block, WavesEnterprise.Transaction[] transactions,long startOffset,long endOffset)
			where TBlock : IBlockWithHeight
		{
			var dbTx = transactions.Select(x => new Database.Data.Tx()
			{
				Height = block.Height,
				ContractId = x.GetContractId(),
				TxId = x.GetTransactionId(),
				NestedTxId = x.GetNestedTxId(),
			}).ToArray();

			_stateStore.StoreBlock(new Database.Data.Block()
			{
				Height = block.Height,
				Signature = block.BlockSignature.ToByteArray(),
				Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(block.Timestamp).UtcDateTime,
				StartOffset = startOffset,
				EndOffset = endOffset
			}, dbTx);
		}
	}
}
