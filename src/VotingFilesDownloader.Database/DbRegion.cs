using System.Collections.Generic;

namespace VotingFilesDownloader.Database
{
	public class DbRegion
	{
		public virtual long Id { get; set; }
		public virtual long ExternalId { get; set; }
		public virtual string Name { get; set; }
		public virtual string Url { get; set; }

		public virtual ISet<DbElection> Elections { get; set; } = new HashSet<DbElection>();
	}
}
