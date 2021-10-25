using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Voting2021.BlockchainWatcher.Services
{
	public sealed class DataSigningService
		: IDataSigningService
	{
		private ECDsa _edsa = null;

		ECCurve ecCurve = new ECCurve() // Ed25519, 32 bytes, 256 bit
		{
			CurveType = ECCurve.ECCurveType.PrimeTwistedEdwards,
			A = new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xec }, // GF(-1)
			B = new byte[] { 0x52, 0x03, 0x6C, 0xEE, 0x2B, 0x6F, 0xFE, 0x73, 0x8C, 0xC7, 0x40, 0x79, 0x77, 0x79, 0xE8, 0x98,
	  0x00, 0x70, 0x0A, 0x4D, 0x41, 0x41, 0xD8, 0xAB, 0x75, 0xEB, 0x4D, 0xCA, 0x13, 0x59, 0x78, 0xA3 },
			G = new ECPoint()
			{
				X = new byte[] { 0x21, 0x69, 0x36, 0xD3, 0xCD, 0x6E, 0x53, 0xFE, 0xC0, 0xA4, 0xE2, 0x31, 0xFD, 0xD6, 0xDC, 0x5C,
		0x69, 0x2C, 0xC7, 0x60, 0x95, 0x25, 0xA7, 0xB2, 0xC9, 0x56, 0x2D, 0x60, 0x8F, 0x25, 0xD5, 0x1A },
				Y = new byte[] { 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66,
		0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x58 }
			},
			Prime = new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xed },
			Order = new byte[] { 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	  0x14, 0xde, 0xf9, 0xde, 0xa2, 0xf7, 0x9c, 0xd6, 0x58, 0x12, 0x63, 0x1a, 0x5c, 0xf5, 0xd3, 0xed },
			Cofactor = new byte[] { 8 }
		};

		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		public DataSigningService()
		{
			if (File.Exists("private.key"))
			{
				var obj = JsonSerializer.Deserialize<Parameters>(Convert.FromBase64String(File.ReadAllText("private.key")));
				_edsa = ECDsa.Create(ecCurve);
				ECParameters parameters = new ECParameters()
				{
					Curve = ecCurve,
					D = obj.D,
					Q = new ECPoint()
					{
						X = obj.X,
						Y = obj.Y
					}
				};
				_edsa.ImportParameters(parameters);
			}
			else
			{
				_edsa = ECDsa.Create(ecCurve);
				_edsa.GenerateKey(ecCurve);
				var parameters = _edsa.ExportParameters(true);

				var export = new Parameters()
				{
					X = parameters.Q.X,
					Y = parameters.Q.Y,
					D = parameters.D
				};
				var json = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(export));
				File.WriteAllText("private.key", json);
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
				byte[] signature = null;
				signature = _edsa.SignHash(hash);
				File.WriteAllBytes(fileName + ".signature", signature);
			}
			catch(Exception e)
			{

			}
			finally
			{
				_semaphore.Release();
			}
		}

		private sealed class Parameters
		{
			[JsonPropertyName("x")]
			public byte[] X { get; set; }

			[JsonPropertyName("y")]
			public byte[] Y { get; set; }

			[JsonPropertyName("d")]
			public byte[] D { get; set; }
		}
	}

	public interface IDataSigningService
	{
		void SignFile(string fileName);
	}
}
