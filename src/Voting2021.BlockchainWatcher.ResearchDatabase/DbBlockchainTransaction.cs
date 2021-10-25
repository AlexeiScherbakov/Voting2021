namespace Voting2021.BlockchainWatcher.ResearchDatabase
{
	public class DbBlockchainTransaction
	{
		public virtual long Id { get; set; }
		public virtual DbBlockchainBlock Block { get; set; }
		public virtual TransationRecordType Type { get; set; }
		public virtual string TxId { get; set; }
		public virtual string NestedTxId { get; set; }
		public virtual string ContractId { get; set; }
		public virtual string OperationType { get; set; }
		public virtual long Timestamp { get; set; }
		public virtual long NestedTimestamp { get; set; }
		public virtual byte[] TransactionBytes { get; set; }
		public virtual string TransactionJson { get; set; }
	}
}
