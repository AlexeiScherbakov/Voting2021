using System.Collections.Generic;

namespace Voting2021.BlockchainWatcher.Services
{



	public abstract class TransactionFormatter
	{
		public abstract byte[] WriteTransaction(string key, byte[] value, Dictionary<string, string> headers);
	}
}
