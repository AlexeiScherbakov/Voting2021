using System;

using NHibernate;
using NHibernate.AdoNet;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Connection;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;

namespace Voting2021.BlockchainWatcher.ResearchDatabase
{
	public class ResearchDatabase
		: IDisposable
	{
		private readonly static HbmMapping _mapping;

		static ResearchDatabase()
		{
			_mapping = CreateMapping();
		}


		private readonly ISessionFactory _sessionFactory;

		private ResearchDatabase(ISessionFactory sessionFactory)
		{
			_sessionFactory = sessionFactory;
		}

		public void Dispose()
		{
			_sessionFactory.Dispose();
		}

		public ISessionFactory SessionFactory
		{
			get { return _sessionFactory; }
		}

		public ISession OpenSession()
		{
			return _sessionFactory.OpenSession();
		}

		public IStatelessSession OpenStatelessSession()
		{
			return _sessionFactory.OpenStatelessSession();
		}

		public static ResearchDatabase Sqlite(string connectionString, bool create)
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

			return new ResearchDatabase(cfg.BuildSessionFactory());
		}

		private static HbmMapping CreateMapping()
		{
			var mapper = new ModelMapper();

			mapper.Class<DbBlockchainBlock>(clazz =>
			{
				clazz.Table("block");
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.Property(x => x.Shard, map =>
				{
					map.Column("shard");
					map.Index("idx_block_shard");
				});
				clazz.Property(x => x.Height, map =>
				{
					map.Column("height");
					map.Index("idx_block_height");
				});
				clazz.Property(x => x.Signature, map =>
				{
					map.Column("signature");
					map.Index("idx_block_signature");
				});
				clazz.Property(x => x.Timestamp, map =>
				{
					map.Column("timestamp");
					map.Index("idx_block_timestamp");
				});
				clazz.Set(x => x.Transactions, map =>
				{
					map.Table("tx");
					map.Key(x => x.Column("block_id"));
					map.Inverse(true);
				}, relation => relation.OneToMany());
			});
			mapper.Class<DbBlockchainTransaction>(clazz =>
			{
				const string TableName = "tx";
				clazz.Table(TableName);
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.ManyToOne(x => x.Block, map =>
				{
					const string ColumnName = "block_id";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.Type, map =>
				{
					const string ColumnName = "record_type";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.TxId, map =>
				{
					const string ColumnName = "tx_id";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.NestedTxId, map =>
				{
					const string ColumnName = "nested_tx_id";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.ContractId, map =>
				{
					const string ColumnName = "contract_id";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.Timestamp, map =>
				{
					const string ColumnName = "timestamp";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.NestedTimestamp, map =>
				{
					const string ColumnName = "nested_timestamp";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.OperationType, map =>
				{
					const string ColumnName = "operation_type";
					map.Column(ColumnName);
					map.Index("idx_" + TableName + "_" + ColumnName);
				});
				clazz.Property(x => x.TransactionBytes, map =>
				{
					map.Column("bytes");
				});
				clazz.Property(x => x.TransactionJson, map =>
				{
					map.Column("json");
				});
			});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}
	}
}
