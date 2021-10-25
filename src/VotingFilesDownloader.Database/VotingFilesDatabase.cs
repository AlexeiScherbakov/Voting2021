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

		public static VotingFilesDatabase Sqlite(string connectionString, Mode mode)
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

			switch (mode)
			{
				case Mode.Create:
					var export = new SchemaExport(cfg);
					export.SetDelimiter(";");
					export.Execute(false, true, false);
					break;
				case Mode.Update:
					var update = new SchemaUpdate(cfg);
					update.Execute(false, true);
					break;
				case Mode.ReadOnly:
					break;
				default:
					throw new InvalidOperationException();
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
					const string ColumnName = "district_id";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
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
					const string ColumnName = "voting_id";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.FileName, map =>
				{
					const string ColumnName = "filename";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
					map.NotNullable(true);
				});
				clazz.Property(x => x.Length, map =>
				{
					const string ColumnName = "length";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
					map.NotNullable(true);
				});
				clazz.Property(x => x.Sha256, map =>
				{
					const string ColumnName = "sha256";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
					map.NotNullable(true);
				});
				clazz.Set(x => x.Transactions, map =>
				{
					map.Table("transaction_in_file");
					map.Key(x => x.Column("file_id"));
					map.Inverse(true);
				}, relation => relation.OneToMany());
			});
			mapper.Class<DbTransactionInFile>(clazz =>
			{
				const string TableName = "transaction_in_file";
				clazz.Table(TableName);
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.ManyToOne(x => x.File, map =>
				{
					const string ColumnName = "file_id";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.NestedTxId, map =>
				{
					const string ColumnName = "nested_tx_id";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.Type, map =>
				{
					const string ColumnName = "type";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.Signature, map =>
				{
					const string ColumnName = "signature";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.Version, map =>
				{
					const string ColumnName = "version";
					map.Column(ColumnName);
				});
				clazz.Property(x => x.Timestamp, map =>
				{
					const string ColumnName = "timestamp";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.SenderPublicKey, map =>
				{
					const string ColumnName = "sender_public_key";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.Fee, map =>
				{
					const string ColumnName = "fee";
					map.Column(ColumnName);
				});
				clazz.Property(x => x.FeeAssetId, map =>
				{
					const string ColumnName = "fee_asset_id";
					map.Column(ColumnName);
				});
				clazz.Property(x => x.Params, map =>
				{
					const string ColumnName = "params";
					map.Column(ColumnName);
				});
				clazz.Property(x => x.Diff, map =>
				{
					const string ColumnName = "diff";
					map.Column(ColumnName);
				});
				clazz.Property(x => x.Extra, map =>
				{
					const string ColumnName = "extra";
					map.Column(ColumnName);
				});
				clazz.Property(x => x.Rollback, map =>
				{
					const string ColumnName = "rollback";
					map.Column(ColumnName);
				});
			});
			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}



		public enum Mode
		{
			None=0,
			Create=1,
			Update=2,
			ReadOnly=3
		}
	}
}
