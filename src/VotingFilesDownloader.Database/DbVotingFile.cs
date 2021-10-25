using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VotingFilesDownloader.Database
{


	public class DbVotingFile
	{
		public virtual long Id { get; set; }
		public virtual DbVoting Voting { get; set; }
		public virtual string FileName { get; set; }
		public virtual long Length { get; set; }
		public virtual byte[] Sha256 { get; set; }

		public virtual ISet<DbTransactionInFile> Transactions { get; protected set; } = new HashSet<DbTransactionInFile>();
	}
}
