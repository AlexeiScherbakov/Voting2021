using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Google.Protobuf;

using Voting2021.BlockchainClient;
using Voting2021.BlockchainClient.ObjectModel;

using WavesEnterprise;

namespace Voting2021.BlockchainWatcher.Services
{
	public interface ITransactionStore
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="headers"></param>
		/// <returns></returns>
		void AddEntry(string key, byte[] value, Dictionary<string, string> headers);

		long GetCurrentOffset();

		Task CopyTo(string fileName, long start, long end, IProgress<(long, long)> progressUpdater = null);
	}



	public static class TransactionStoreExtension
	{
		public static void SendBlock(this ITransactionStore @this,
			IBlockWithHeight block,
			Transaction[] transactions)
		{
			foreach (var transaction in transactions)
			{
				var value = transaction.ToByteArray();

				var key = Convert.ToHexString(transaction.GetContractId());
				var id = Convert.ToHexString(transaction.GetTransactionId());
				var nested = transaction.GetNestedTx();

				Dictionary<string, string> headers = new(6);
				headers.Add("event", "transaction");
				headers.Add("contractId", key);
				headers.Add("txId", key);
				headers.Add("id", id);
				if (nested != null)
				{
					var nestedId = Convert.ToHexString(nested.Id.ToByteArray());
					headers.Add("nestedId", nestedId);
				}
				headers.Add("height", block.Height.ToString());
				@this.AddEntry("transaction::" + key,
					value, headers);
			}
		}

		public static void SendRollback(this ITransactionStore @this, Database.Data.Tx[] transactions)
		{
			var value = Array.Empty<byte>();
			foreach (var transaction in transactions)
			{
				var key = Convert.ToHexString(transaction.ContractId);
				var id = Convert.ToHexString(transaction.TxId);

				Dictionary<string, string> headers = new(6);
				headers.Add("event", "rollback");
				headers.Add("contractId", key);
				headers.Add("txId", key);
				headers.Add("id", id);
				if (transaction.NestedTxId != null)
				{
					var nestedId = Convert.ToHexString(transaction.NestedTxId);
					headers.Add("nestedId", nestedId);
				}
				headers.Add("height", transaction.Height.ToString());
				@this.AddEntry("transaction::" + key + "::rollback",
					value, headers);
			}
		}
	}
}
