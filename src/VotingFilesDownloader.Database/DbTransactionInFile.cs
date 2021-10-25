namespace VotingFilesDownloader.Database
{
	public class DbTransactionInFile
	{
		public virtual long Id { get; set; }

		public virtual DbVotingFile File { get; set; }

		public virtual string NestedTxId { get; set; }

		public virtual int Type { get; set; }

		public virtual string Signature { get; set; }

		public virtual string Version { get; set; }

		public virtual long Timestamp { get; set; }

		public virtual string SenderPublicKey { get; set; }

		public virtual string Fee { get; set; }

		public virtual string FeeAssetId { get; set; }

		public virtual string Params { get; set; }

		public virtual string Diff { get; set; }

		public virtual string Extra { get; set; }

		public virtual string Rollback { get; set; }
	}
}
