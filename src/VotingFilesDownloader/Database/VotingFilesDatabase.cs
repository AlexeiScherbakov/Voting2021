using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Connection;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VotingFilesDownloader.Database
{
	public class VotingFilesDatabase
	{
		private readonly static HbmMapping _mapping;


		static VotingFilesDatabase()
		{
			_mapping = CreateMapping();
		}

		private readonly ISessionFactory _sessionFactory;

		private VotingFilesDatabase(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory;
		}

		public void Dispose()
		{
			_sessionFactory.Dispose();
		}

		public ISession OpenSession()
		{
			return _sessionFactory.OpenSession();
		}

		public static VotingFilesDatabase Sqlite(string connectionString, bool create)
		{
			var cfg = new Configuration();
			cfg.DataBaseIntegration(
				db =>
				{
					db.MaximumDepthOfOuterJoinFetching = 6;
					db.ConnectionString = connectionString;
					db.ConnectionProvider<DriverConnectionProvider>();
					db.Driver<NHibernate.Driver.SQLite20Driver>();
					db.Dialect<NHibernate.Dialect.SQLiteDialect>();
				}).Proxy(x => x.ProxyFactoryFactory<NHibernate.Bytecode.StaticProxyFactoryFactory>());
			cfg.AddMapping(_mapping);

			if (create)
			{
				var export = new SchemaExport(cfg);
				export.SetDelimiter(";");
				export.Execute(false, true, false);
			}

			return new VotingFilesDatabase(cfg.BuildSessionFactory());
		}


		private static HbmMapping CreateMapping()
		{
			var mapper = new ModelMapper();

			mapper.Class<DbRegion>(clazz =>
			{
				clazz.Table("region");
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.Property(x => x.ExternalId, map =>
				{
					map.Column("external_id");
				});
				clazz.Property(x => x.Name, map =>
				{
					map.Column("name");
					map.NotNullable(true);
				});
				clazz.Property(x => x.Url, map =>
				{
					map.Column("url");
				});
				clazz.Set(x => x.Elections, map =>
				{
					map.Table("election");
					map.Key(x => x.Column("region_id"));
					map.Inverse(true);
				}, relation => relation.OneToMany());
			});
			mapper.Class<DbElection>(clazz =>
			{
				clazz.Table("election");
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.ManyToOne(x => x.Region, map =>
				{
					map.Column("region_id");
				});
				clazz.Property(x => x.ExternalId, map =>
				{
					map.Column("external_id");
				});
				clazz.Property(x => x.Name, map =>
				{
					map.Column("name");
					map.NotNullable(true);
				});
				clazz.Set(x => x.Districts, map =>
				{
					map.Table("district");
					map.Key(x => x.Column("election_id"));
					map.Inverse(true);
				}, relation => relation.OneToMany());
			});
			mapper.Class<DbDistrict>(clazz =>
			{
				clazz.Table("district");
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.ManyToOne(x => x.Election, map =>
				{
					map.Column("election_id");
				});
				clazz.Property(x => x.ExternalId, map =>
				{
					map.Column("external_id");
				});
				clazz.Property(x => x.Name, map =>
				{
					map.Column("name");
					map.NotNullable(true);
				});
				clazz.Set(x => x.Votings, map =>
				{
					map.Table("voting");
					map.Key(x => x.Column("district_id"));
					map.Inverse(true);
				}, relation => relation.OneToMany());
			});
			mapper.Class<DbVoting>(clazz =>
			{
				const string TableName = "voting";
				clazz.Table(TableName);
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.ManyToOne(x => x.District, map =>
				{
					map.Column("district_id");
					map.Index(TableName + "_district_id");
				});
				clazz.Property(x => x.Name, map =>
				{
					map.Column("name");
					map.NotNullable(true);
				});
				clazz.Property(x => x.ContractId, map =>
				{
					map.Column("contract_id");
					map.NotNullable(true);
				});
				clazz.Set(x => x.VotingFiles, map =>
				{
					map.Table("voting_file");
					map.Key(x => x.Column("voting_id"));
					map.Inverse(true);
				}, relation => relation.OneToMany());
			});
			mapper.Class<DbVotingFile>(clazz =>
			{
				const string TableName = "voting_file";
				clazz.Table(TableName);
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.ManyToOne(x => x.Voting, map =>
				{
					map.Column("voting_id");
					map.Index(TableName+"_voting_id");
				});
				clazz.Property(x => x.FileName, map =>
				{
					map.Column("filename");
					map.Index(TableName+"_filename");
					map.NotNullable(true);
				});
				clazz.Property(x => x.Length, map =>
				{
					map.Column("length");
					map.Index(TableName + "_length");
					map.NotNullable(true);
				});
				clazz.Property(x => x.Sha256, map =>
				{
					map.Column("sha256");
					map.Index("idx_voting_file_sha256");
					map.Index(TableName + "_sha256");
					map.NotNullable(true);
				});
			});
			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}
	}
}
