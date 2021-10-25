using System;
using System.Collections.Generic;

namespace Voting2021.BlockchainWatcher.ResearchDatabase
{
	public class DbBlockchainBlock
	{
		public virtual long Id { get; set; }
		public virtual int Shard { get; set; }
		public virtual long Height { get; set; }
		public virtual string Signature { get; set; }
		public virtual DateTime Timestamp { get; set; }
		public virtual ISet<DbBlockchainTransaction> Transactions { get; protected set; } = new HashSet<DbBlockchainTransaction>();
	}
}
