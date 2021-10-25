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

	public class DbElection
	{
		public virtual long Id { get; set; }
		public virtual DbRegion Region { get; set; }
		public virtual string ExternalId { get; set; }
		public virtual string Name { get; set; }

		public virtual ISet<DbDistrict> Districts { get; set; } = new HashSet<DbDistrict>();
	}
}
