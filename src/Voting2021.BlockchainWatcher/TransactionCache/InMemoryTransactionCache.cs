using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using WavesEnterprise;

namespace Voting2021.BlockchainWatcher.Services
{
	public sealed class InMemoryTransactionCache
		: ITransactionCache, IDisposable
	{
		private int _poolTtl = 300000;
		private int _cacheClearDelay = 10000;

		private bool _autodeleteTransaction = true;

		private readonly ConcurrentDictionary<string, CachedTransaction> _dictionary = new();

		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		public InMemoryTransactionCache(Microsoft.Extensions.Hosting.IHostApplicationLifetime hostApplicationLifetime)
		{
			Task.Factory.StartNew(CacheCleaner, TaskCreationOptions.LongRunning);
			hostApplicationLifetime.ApplicationStopped.Register(ApplicationStopped);
		}

		public void Dispose()
		{
			_cancellationTokenSource.Dispose();
		}

		private void ApplicationStopped()
		{
			_cancellationTokenSource.Cancel();
		}

		private async Task CacheCleaner()
		{
			while (!_cancellationTokenSource.IsCancellationRequested)
			{
				var now = DateTime.Now;
				foreach (var pair in _dictionary)
				{
					var elapsed = now - pair.Value.Timestamp;
					if (elapsed.TotalMilliseconds > _poolTtl)
					{
						_dictionary.TryRemove(pair.Key, out _);
					}
				}
				try
				{
					await Task.Delay(_cacheClearDelay, _cancellationTokenSource.Token);
				}
				catch (Exception e)
				{

				}
			}
		}

		public Transaction GetTransactionById(byte[] id)
		{
			var hexId = Convert.ToHexString(id);
			CachedTransaction tmp = null;
			bool ok = _autodeleteTransaction ? _dictionary.TryRemove(hexId, out tmp) : _dictionary.TryGetValue(hexId, out tmp);

			if (!ok)
			{
				return null;
			}

			return tmp.Transaction;
		}

		public Transaction[] GetTransactionsById(params byte[][] ids)
		{
			Transaction[] ret = new Transaction[ids.Length];
			for (int i = 0; i < ids.Length; i++)
			{
				ret[i] = GetTransactionById(ids[i]);
			}
			return ret;
		}

		public void Put(byte[] id, Transaction tx)
		{
			var hexId = Convert.ToHexString(id);
			var cached = new CachedTransaction(tx);
			_dictionary.TryAdd(hexId, cached);
		}

		private sealed class CachedTransaction
		{
			private readonly DateTime _timestamp;
			private readonly Transaction _transaction;

			public CachedTransaction(Transaction transaction)
			{
				_timestamp = DateTime.Now;
				_transaction = transaction;
			}

			public DateTime Timestamp
			{
				get { return _timestamp; }
			}


			public Transaction Transaction
			{
				get { return _transaction; }
			}
		}
	}
}
