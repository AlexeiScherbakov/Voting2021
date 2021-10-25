
using WavesEnterprise;

namespace Voting2021.BlockchainWatcher.Services
{


	public interface ITransactionCache
	{
		Transaction GetTransactionById(byte[] id);
		Transaction[] GetTransactionsById(params byte[][] ids);

		void Put(byte[] id, Transaction tx);
	}
}
