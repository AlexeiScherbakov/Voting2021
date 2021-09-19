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

	public class DbDistrict
	{
		public virtual long Id { get; set; }
		public virtual DbElection Election { get; set; }
		public virtual string ExternalId { get; set; }
		public virtual string Name { get; set; }

		public virtual ISet<DbVoting> Votings { get; set; } = new HashSet<DbVoting>();
	}
}
