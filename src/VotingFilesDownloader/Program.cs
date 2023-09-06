using CommandLine;

using NHibernate.Linq;
using NHibernate.Mapping;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

using VotingFilesDownloader.Api;
using VotingFilesDownloader.Database;

namespace VotingFilesDownloader
{
	class Program
	{
		private static string _filesDirectory;
		private static VotingFilesDatabase _votingFilesDatabase;
		private static ApiClient _apiClient;
		private static bool _redownloadAll;

		private static DateTimeOffset _startTime;

		static int Main(string[] args)
		{
			_startTime = DateTimeOffset.Now;
			var parser = new Parser(config => config.HelpWriter = Console.Out);
			var ret = parser.ParseArguments<CommandLineOptions>(args)
				.MapResult(x => RunApp(x), err => 1);
			return ret;
		}

		private static string GetCurrentDirectory()
		{
			var path = Path.GetDirectoryName(typeof(Program).Assembly.Location);
			if (path is null)
			{
				path = AppContext.BaseDirectory;
			}

			return path;
		}

		private static void Initialize()
		{
			var path = GetCurrentDirectory();
			Console.WriteLine("Current Directory: {0}", path);
			var dataDirectory = Path.Combine(path, "data");
			if (!Directory.Exists(dataDirectory))
			{
				Directory.CreateDirectory(dataDirectory);
			}

			_filesDirectory = Path.Combine(dataDirectory, "files");
			var dbFileName = Path.Combine(dataDirectory, "votings.db3");
			System.Data.SQLite.SQLiteConnectionStringBuilder b = new()
			{
				BinaryGUID = true,
				ForeignKeys = true,
				DataSource = dbFileName
			};
			var connectionString = b.ToString();
			bool create = !File.Exists(dbFileName);
			_votingFilesDatabase = VotingFilesDatabase.Sqlite(connectionString, create ? VotingFilesDatabase.Mode.Create : VotingFilesDatabase.Mode.Update);
			_apiClient = new ApiClient();
		}

		private static int RunApp(CommandLineOptions options)
		{
			if (options.Token is not null)
			{
				WriteTokenInfo(options.Token);
			}
			_redownloadAll = options.RedownloadAll;
			Initialize();


			var cert = _apiClient.GetCertificate().GetAwaiter().GetResult();
			Console.WriteLine($"Server certificate {cert.Item1},{cert.Item2}");

			if (options.Token is not null)
			{
				_apiClient.SetAuthorization(options.Token);
			}

			if (!string.IsNullOrEmpty(options.ContractId))
			{
				DownloadContractFiles(options.ContractId).Wait();
			}
			else
			{
				UpdateMetadata(39, 40, 46, 53, 60, 70, 76).Wait();
				DownloadFiles().Wait();
			}
			
			Console.WriteLine("Время выполнения {0}", DateTimeOffset.Now - _startTime);
			return 0;
		}

		private static void WriteTokenInfo(string token)
		{
			var moscowTimeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow UTC+3", TimeSpan.FromHours(3), "UTC+3", "UTC+3");
			JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
			var jwtToken=handler.ReadJwtToken(token);
			var expiration = jwtToken.Payload.Exp;
			Console.WriteLine("IssuedAt: {0}, Exp={1}",
				TimeZoneInfo.ConvertTime(new DateTimeOffset(jwtToken.IssuedAt, TimeSpan.Zero), moscowTimeZone),
				TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(expiration ?? 0), moscowTimeZone));
		}

		public static async Task UpdateMetadata(params long[] startRegions)
		{
			// парсим инкрементально так как есть ограничение на API

			// Region+Election
			using (var session = _votingFilesDatabase.OpenSession())
			{
				foreach (var region in startRegions)
				{
					using (var tr = session.BeginTransaction())
					{
						var regionResponse = await _apiClient.GetElections(region);
						DbRegion dbRegion = session.Query<DbRegion>()
							.Where(x => x.ExternalId == regionResponse.Data.RegionCode)
							.SingleOrDefault();
						if (dbRegion is null)
						{
							dbRegion = new DbRegion()
							{
								ExternalId = regionResponse.Data.RegionCode,
								Name = regionResponse.Data.RegionName
							};
							session.Save(dbRegion);
						}
						Console.WriteLine("Регион {0}", dbRegion.Name);
						foreach (var electionGroup in regionResponse.Data.Elections)
						{
							foreach (var election in electionGroup.Elections)
							{
								DbElection dbElection = dbRegion.Elections.Where(x => x.ExternalId == election.ElectionId)
									.SingleOrDefault();
								if (dbElection is null)
								{
									dbElection = new DbElection()
									{
										ExternalId = election.ElectionId,
										Name = election.ElectionName,
										Region = dbRegion
									};
									session.Save(dbElection);
								}
								Console.WriteLine("Выборы {0}", dbElection.Name);
							}
						}
						tr.Commit();
					}
				}
			}

			// districts
			using (var session = _votingFilesDatabase.OpenSession())
			{
				var dbElections = session.Query<DbElection>().ToArray();
				foreach (var dbElection in dbElections)
				{
					if (dbElection.Districts.Count != 0)
					{
						continue;
					}
					using (var tr = session.BeginTransaction())
					{
						var districts = await _apiClient.GetDistricts(dbElection.Region.ExternalId, dbElection.ExternalId);

						foreach (var district in districts.Data)
						{
							DbDistrict dbDistrict = dbElection.Districts.Where(x => x.ExternalId == district.Id)
								.SingleOrDefault();
							if (dbDistrict is null)
							{
								dbDistrict = new DbDistrict()
								{
									ExternalId = district.Id,
									Name = district.Name,
									Election = dbElection
								};
								session.Save(dbDistrict);
							}
							Console.WriteLine("Округ {0}", dbDistrict.Name);
						}

						tr.Commit();

					}
				}
			}

			// voting
			using (var session = _votingFilesDatabase.OpenSession())
			{
				var dbDistricts = session.Query<DbDistrict>().ToArray();
				foreach(var dbDistrict in dbDistricts)
				{
					if (dbDistrict.Votings.Count != 0)
					{
						continue;
					}
					var votingResponse = await _apiClient.GetDistrictsVotings(dbDistrict.ExternalId);
					using (var tr = session.BeginTransaction())
					{
						foreach(var voting in votingResponse.Data.Votings)
						{
							Console.WriteLine("Голосование {0}", voting.Counters?.Name);
							DbVoting dbVoting = new DbVoting()
							{
								District = dbDistrict,
								Name = voting.Counters.Name,
								ContractId = voting.Counters.ContractId,
							};
							session.Save(dbVoting);
						}
						tr.Commit();
					}	
				}
			}
		}

		public static async Task DownloadFiles()
		{
			// Нам уже пофиг на всю эту иерархию просто пробегаем по всем контрактам
			string[] contracts = null;
			using (var session = _votingFilesDatabase.OpenSession())
			{
				contracts = session.Query<DbVoting>()
					.Select(x => x.ContractId)
					.Distinct()
					.ToArray();
			}


			foreach(var contract in contracts)
			{
				await DownloadContractFiles(contract);
			}
		}

		public static async Task DownloadContractFiles(string contract)
		{
			var files = await _apiClient.GetContractTransactionsFiles(contract);
			using (var session = _votingFilesDatabase.OpenSession())
			using (var tr = session.BeginTransaction())
			{
				var dbVoting = session.Query<DbVoting>()
					.Where(x => x.ContractId == contract)
					.Fetch(x => x.VotingFiles)
					.SingleOrDefault();

				if (dbVoting is null)
				{
					Console.WriteLine("Для использования этой функции необходимо вначале скачать список голосований");
					return;
				}
				foreach (var file in files.Data)
				{
					var dbVotingFile = dbVoting.VotingFiles.Where(x => x.FileName == file)
						.SingleOrDefault();
					if (dbVotingFile is null)
					{
						var data = await _apiClient.DownloadTransactionFile(contract, file);
						Console.WriteLine("File {0}", file);
						var hash = CalculateSha256(data);

						dbVotingFile = new DbVotingFile()
						{
							Voting = dbVoting,
							FileName = file,
							Length = data.Length,
							Sha256 = hash
						};
						session.Save(dbVotingFile);

						SaveFile(contract, file, data);
					}
					else
					{
						if (!_redownloadAll)
						{
							continue;
						}
						var data = await _apiClient.DownloadTransactionFile(contract, file);
						Console.WriteLine("File {0}", file);
						var hash = CalculateSha256(data);

						if (!MemoryExtensions.SequenceEqual<byte>(hash, dbVotingFile.Sha256))
						{
							Console.WriteLine("Detected anomaly {0}", file);
							SaveFile(contract, file + "-anomaly" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"), data);
						}
					}
				}
				tr.Commit();
			}
		}

		public static byte[] CalculateSha256(byte[] data)
		{
			using var hasher = SHA256.Create();
			return hasher.ComputeHash(data);
		}

		public static void SaveFile(string contractId,string fileName,byte[] data)
		{
			var contractTransactionFileDirectory = Path.Combine(_filesDirectory, contractId);
			if (!Directory.Exists(contractTransactionFileDirectory))
			{
				Directory.CreateDirectory(contractTransactionFileDirectory);
			}
			var fullFileName = Path.Combine(contractTransactionFileDirectory, fileName);
			File.WriteAllBytes(fullFileName, data);
		}
	}
}
