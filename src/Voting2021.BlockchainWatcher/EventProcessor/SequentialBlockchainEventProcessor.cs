using System;
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
	public sealed class SequentialBlockchainEventProcessor
		: IBlockchainEventProcessor
	{
		private readonly ILogger<SequentialBlockchainEventProcessor> _logger;

		private ITransactionStore _transactionStore;
		private ITransactionCache _transactionCache;
		private Database.StateStore _stateStore;

		public SequentialBlockchainEventProcessor(
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
			var ret = _stateStore.GetLastBlockInfo();

			return (ret.signature, ret.height, ret.transactionCount);
		}

		public void ProcessBlockAppended(WavesEnterprise.BlockAppended blockAppended)
		{
			var transactionIds = blockAppended.TxIds?.Select(x => x.ToByteArray()).ToArray() ?? Array.Empty<byte[]>();
			var transactions = _transactionCache.GetTransactionsById(transactionIds);
			_transactionStore.SendBlock(blockAppended, transactions);
			SaveBlock(blockAppended, transactions);
		}

		public void ProcessAppendedBlockHistory(WavesEnterprise.AppendedBlockHistory appendedBlockHistory)
		{
			var transactions = appendedBlockHistory.Txs.Select(x => x).ToArray();
			_transactionStore.SendBlock(appendedBlockHistory, transactions);
			SaveBlock(appendedBlockHistory, transactions);
		}

		public void ProcessMicroBlockAppended(WavesEnterprise.MicroBlockAppended microBlockAppended)
		{
			foreach (var tx in microBlockAppended.Txs)
			{
				var id = tx.GetTransactionId();
				_transactionCache.Put(id, tx);
			}
		}

		public void ProcessRollbackCompleted(WavesEnterprise.RollbackCompleted rollbackCompleted)
		{
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

			_logger.LogInformation("Deleted {DeletedTransactionCount} transactions from state store", deletedTransactions);

			tr.Commit();
		}

		private void SaveBlock<TBlock>(TBlock block, WavesEnterprise.Transaction[] transactions)
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
			}, dbTx);
		}
	}
}
