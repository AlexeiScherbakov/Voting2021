using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voting2021.BlockchainWatcher.Settings
{
	public class BlockchainConnectionSettings
	{
		public string ConnectionUrl { get; set; }
	}


	public sealed class TransactionStoreSettings
	{
		public string DatabaseFile { get; set; }
		public string TransactionLogFile { get; set; }
	}


	public sealed class AppIdSettings
	{
		public string AppId { get; set; }
	}
}
