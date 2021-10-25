using System;

using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Connection;
using NHibernate.Mapping.ByCode;
using System.Linq;
using NHibernate.Tool.hbm2ddl;
using Voting2021.Database.Data;

namespace Voting2021.Database
{
	public class StateStore
		: IDisposable
	{
		private readonly static HbmMapping _mapping;

		static StateStore()
		{
			_mapping = CreateMapping();
		}


		private readonly ISessionFactory _sessionFactory;

		private StateStore(ISessionFactory sessionFactory)
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


		public (byte[] signature, long height, long transactionCount) GetLastBlockInfo()
		{
			using var session = _sessionFactory.OpenSession();
			session.DefaultReadOnly = true;
			var ret = session.Query<Data.Block>()
				.OrderByDescending(x => x.Height)
				.Select(x => new
				{
					x.Signature,
					x.Height
				})
				.FirstOrDefault();

			var trCount = session.Query<Data.Tx>().Count();

			if (ret is null)
			{
				return (null, 0, 0);
			}
			return (ret.Signature, ret.Height, trCount);
		}

		public void StoreBlock(Block block,Tx[] transactions)
		{
			using var session = _sessionFactory.OpenSession();
			using var tr = session.BeginTransaction();

			session.Save(block);
			foreach(var tx in transactions)
			{
				session.Save(tx);
			}


			tr.Commit();
		}

		public static StateStore Sqlite(string connectionString,bool create)
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

			return new StateStore(cfg.BuildSessionFactory());
		}

		private static HbmMapping CreateMapping()
		{
			var mapper = new ModelMapper();

			mapper.Class<Data.Block>(clazz =>
			{
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.Property(x => x.Height, map =>
				{
					map.Column("height");
					map.Index("idx_block_height");
				});
				clazz.Property(x => x.Signature, map =>
				{
					map.Column("signature");
				});
				clazz.Property(x => x.Timestamp, map =>
				{
					map.Column("time_stamp");
				});
				clazz.Property(x => x.DeletedAt, map =>
				{
					map.Column("deleted_at");
				});
				clazz.Property(x => x.StartOffset, map =>
				{
					map.Column("start_offset");
				});
				clazz.Property(x => x.EndOffset, map =>
				{
					map.Column("end_offset");
				});
			});
			mapper.Class<Data.Tx>(clazz =>
			{
				clazz.Id(x => x.Id, map =>
				{
					map.Column("id");
					map.Generator(Generators.Native);
				});
				clazz.Property(x => x.Height, map =>
				{
					map.Column("height");
					map.Index("idx_tx_height");
				});
				clazz.Property(x => x.TxId, map =>
				{
					map.Column("tx_id");
				});
				clazz.Property(x => x.NestedTxId, map =>
				{
					map.Column("nested_tx_id");
				});
				clazz.Property(x => x.ContractId, map =>
				{
					map.Column("contract_id");
				});
				clazz.Property(x => x.DeletedAt, map =>
				{
					map.Column("deleted_at");
					map.Index("idx_tx_deleted_at");
				});
			});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}
	}
}
