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

	public class DbVoting
	{
		public virtual long Id { get; set; }
		public virtual DbDistrict District { get; set; }
		public virtual string Name { get; set; }
		public virtual string ContractId { get; set; }

		public virtual ISet<DbVotingFile> VotingFiles { get; set; } = new HashSet<DbVotingFile>();
	}
}
