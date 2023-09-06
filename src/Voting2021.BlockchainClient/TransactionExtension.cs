using System.Linq;

using WavesEnterprise;

namespace Voting2021.BlockchainClient
{
	public static class TransactionExtension
	{
		public static ObjectModel.ITransactionWithId GetNestedTx(this WavesEnterprise.Transaction transaction)
		{
			var executableTransaction = transaction?.ExecutedContractTransaction?.Tx;
			if (executableTransaction is null)
			{
				return null;
			}
			if (executableTransaction.CallContractTransaction is not null)
			{
				return executableTransaction.CallContractTransaction;
			}
			if (executableTransaction.CreateContractTransaction is not null)
			{
				return executableTransaction.CreateContractTransaction;
			}
			if (executableTransaction.UpdateContractTransaction is not null)
			{
				return executableTransaction.UpdateContractTransaction;
			}

			return null;
		}

		public static byte[] GetContractId(this WavesEnterprise.Transaction transaction)
		{
			var nested = GetNestedTx(transaction);
			if (nested != null)
			{
				if (nested is CallContractTransaction cct)
				{
					return cct.ContractId.ToByteArray();
				}
				return nested.Id.ToByteArray();
			}
			return transaction.GetTransactionId();
		}

		public static byte[] GetNestedTxId(this WavesEnterprise.Transaction transaction)
		{
			var nested = GetNestedTx(transaction);
			if (nested != null)
			{
				return nested.Id.ToByteArray();
			}
			return transaction.GetTransactionId();
		}

		public static string GetOperationType(this WavesEnterprise.Transaction tx)
		{
			if (tx.ExecutedContractTransaction is not null)
			{
				var inner = tx.ExecutedContractTransaction.Tx;
				if (inner.CreateContractTransaction is not null)
				{
					return string.Format("Executed CreateContractTransaction {0}", inner.CreateContractTransaction.ContractName);
				}
				if (inner.CallContractTransaction is not null)
				{
					var operationName = inner.CallContractTransaction.Params.Where(x => x.Key == "operation").SingleOrDefault();
					return string.Format("Executed CallContractTransaction {0}", operationName?.StringValue);
				}
				if (inner.UpdateContractTransaction is not null)
				{
					return "Executed UpdateContractTransaction";
				}
				return "Unknown ExecutedContractTransaction";
			}
			if (tx.CreateContractTransaction is not null)
			{
				return "CreateContractTransaction";
			}
			if (tx.CallContractTransaction is not null)
			{
				return "CallContractTransaction";
			}
			return "unknown";
		}
	}
}
