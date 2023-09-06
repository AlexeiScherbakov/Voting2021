
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Google.Protobuf;

using Voting2021.BlockchainClient;
using Voting2021.BlockchainWatcher.ResearchDatabase;

namespace BlockchainVerifier
{
	public class BlockchainDataImporter
	{
		private ResearchDatabase _researchDatabase;

		private readonly BufferBlock<ImportObj> _bufferBlock;
		private readonly BatchBlock<ImportObj> _batchBlock;
		private readonly ActionBlock<ImportObj[]> _importBlock;

		public BlockchainDataImporter(ResearchDatabase researchDatabase)
		{
			_researchDatabase = researchDatabase;

			_bufferBlock = new BufferBlock<ImportObj>(new DataflowBlockOptions()
			{
			});

			_batchBlock = new BatchBlock<ImportObj>(100_000, new GroupingDataflowBlockOptions()
			{
				
			});

			_importBlock = new ActionBlock<ImportObj[]>(ImportAction, new ExecutionDataflowBlockOptions()
			{
				MaxDegreeOfParallelism = 1
			});

			_bufferBlock.LinkTo(_batchBlock, new DataflowLinkOptions()
			{
				
			});
			_batchBlock.LinkTo(_importBlock, new DataflowLinkOptions()
			{
			});
		}

		public async Task WaitUntilCompletedAsync()
		{
			await WaitOnBlock(_bufferBlock);
			_batchBlock.TriggerBatch();
			await WaitOnBlock(_batchBlock);
			await WaitOnBlock(_importBlock);
		}

		private async Task WaitOnBlock<TBlock>(TBlock block)
			where TBlock : IDataflowBlock
		{
			block.Complete();
			await block.Completion;
		}

		List<DbBlockchainTransaction> _transactionListBuffer = new(100000);

		private void ImportAction(ImportObj[] objs)
		{
			var heights = objs.GroupBy(x => x.Shard)
				.Select(x => new
				{
					x.Key,
					Items = x.Select(y => y.Height).Distinct()
				}).ToDictionary(x => x.Key, x => x.Items.ToArray());

			using var session = _researchDatabase.OpenStatelessSession();
			using var tr = session.BeginTransaction();

			// подгружаем блоки
			Dictionary<int, Dictionary<long, DbBlockchainBlock>> cache = new();
			foreach (var pair in heights)
			{
				int shard = pair.Key;
				long[] h = pair.Value;
				var dic = session.Query<DbBlockchainBlock>()
					.Where(x => x.Shard == shard)
					.Where(x => h.Contains(x.Height))
					.ToDictionary(x => x.Height, x => x);
				cache.Add(shard, dic);
			}
			_transactionListBuffer.Clear();
			JsonFormatter formatter = new JsonFormatter(new JsonFormatter.Settings(false));
			Parallel.ForEach(objs, obj =>
			 {
				 var dbShard = cache[obj.Shard];

				 if (!dbShard.TryGetValue(obj.Height,out var dbBlock))
				 {
					 Console.WriteLine("Height not found {0} (not final dump)", obj.Height);
					 return; 
				 }

				 DbBlockchainTransaction tx = new DbBlockchainTransaction()
				 {
					 Block = dbBlock,
					 TransactionBytes = obj.Transaction.ToByteArray(),
					 TransactionJson = formatter.Format(obj.Transaction),
					 NestedTxId = Base58.EncodePlain(obj.Transaction.GetNestedTxId()),
					 TxId = Base58.EncodePlain(obj.Transaction.GetTransactionId()),
					 ContractId = Base58.EncodePlain(obj.Transaction.GetContractId()),
					 Timestamp = obj.Transaction.GetTimestamp(),
					 NestedTimestamp = obj.Transaction.GetNestedTx()?.Timestamp ?? 0,
					 OperationType = obj.Transaction.GetOperationType(),
					 Type = obj.IsRollback ? TransationRecordType.Rollback : TransationRecordType.Transaction
				 };
				 lock (_transactionListBuffer)
				 {
					 _transactionListBuffer.Add(tx);
				 }
			 });
			for (int i = 0; i < _transactionListBuffer.Count; i++)
			{
				session.Insert(_transactionListBuffer[i]);

			}
			_transactionListBuffer.Clear();
			tr.Commit();
		}

		

		public void AddTransaction(int shard,WavesEnterprise.Transaction transaction,Dictionary<string,string> headers)
		{
			long height = long.Parse(headers["height"]);
			var ev = headers["event"];
			bool rollback = false;
			if (ev == "rollback")
			{
				rollback = true;
			}
			var obj = new ImportObj()
			{
				Shard = shard,
				Height = height,
				IsRollback = rollback,
				Transaction = transaction
			};
			bool ok=_bufferBlock.Post(obj);
			if (ok == false)
			{

			}
		}

		private sealed class ImportObj
		{
			public int Shard;
			public WavesEnterprise.Transaction Transaction;
			public long Height;
			public bool IsRollback;
		}
	}
}
