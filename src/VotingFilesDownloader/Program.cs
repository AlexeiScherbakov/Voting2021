using NHibernate.Linq;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
		static void Main(string[] args)
		{
			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var dataDirectory = Path.Combine(path, "data");
			if (!Directory.Exists(dataDirectory))
			{
				Directory.CreateDirectory(dataDirectory);
			}
			_filesDirectory = Path.Combine(dataDirectory, "files");
			var dbFileName = Path.Combine(dataDirectory, "votings.db3");
			System.Data.SQLite.SQLiteConnectionStringBuilder b = new();
			b.BinaryGUID = true;
			b.ForeignKeys = true;
			b.DataSource = dbFileName;
			var connectionString = b.ToString();
			bool create = !File.Exists(dbFileName);
			_votingFilesDatabase = VotingFilesDatabase.Sqlite(connectionString, create);
			_apiClient = new ApiClient();

			UpdateMetadata(46, 51, 52, 61, 76, 94).Wait();
			DownloadFiles().Wait();
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
				var files = await _apiClient.GetContractTransactionsFiles(contract);
				using (var session = _votingFilesDatabase.OpenSession())
				using (var tr=session.BeginTransaction())
				{
					var dbVoting = session.Query<DbVoting>()
						.Where(x => x.ContractId == contract)
						.Fetch(x => x.VotingFiles)
						.Single();
					foreach(var file in files.Data)
					{
						var data = await _apiClient.DownloadTransactionFile(contract, file);
						Console.WriteLine("File {0}", file);
						var hash = CalculateSha256(data);
						var dbVotingFile = dbVoting.VotingFiles.Where(x => x.FileName == file)
							.SingleOrDefault();
						if (dbVotingFile is null)
						{
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
							if (!MemoryExtensions.SequenceEqual<byte>(hash, dbVotingFile.Sha256))
							{
								Console.WriteLine("Detected anomaly {0}", file);
							}
							SaveFile(contract, file + "-anomaly" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"), data);
						}
					}

					tr.Commit();
				}
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
