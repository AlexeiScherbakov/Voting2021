using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

using NSec.Cryptography;

namespace Voting2021.BlockchainWatcher.Services
{


	public sealed class DataSigningServiceNSec
		:IDataSigningService
	{
		private SignatureAlgorithm _alg;
		private Key _key;

		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		public DataSigningServiceNSec()
		{
			_alg = SignatureAlgorithm.Ed25519;

			if (File.Exists("nsecprivate.key"))
			{
				var keyBytes = Convert.FromBase64String(File.ReadAllText("nsecprivate.key"));
				_key=Key.Import(_alg, keyBytes, KeyBlobFormat.RawPrivateKey);
			}
			else
			{
				_key= Key.Create(_alg, new KeyCreationParameters()
				{
					ExportPolicy = KeyExportPolicies.AllowPlaintextExport
				});

				var keyBytes = _key.Export(KeyBlobFormat.RawPrivateKey);
				File.WriteAllText("nsecprivate.key",Convert.ToBase64String(keyBytes));
			}
		}

		public void SignFile(string fileName)
		{
			try
			{
				_semaphore.Wait();
				using var hasher = SHA256.Create();
				using var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.SequentialScan);
				var hash = hasher.ComputeHash(file);
				byte[] signature = _alg.Sign(_key, hash);
				File.WriteAllBytes(fileName + ".signature", signature);
			}
			catch (Exception e)
			{

			}
			finally
			{
				_semaphore.Release();
			}
		}
	}
}
