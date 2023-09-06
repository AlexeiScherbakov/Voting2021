using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VotingFilesDownloader.Api
{
	public sealed class ApiClient
		: IDisposable
	{
		private readonly Uri _baseUrl = new Uri("https://stat.vybory.gov.ru/");

		private readonly HttpClient _httpClient;
		private readonly bool _ownsHttpClient;

		private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions();

		private string _bearerToken;

		public ApiClient()
		{
			HttpClientHandler handler = new HttpClientHandler();
			CookieContainer container = new CookieContainer();
			//container.Add(new Cookie(
			//	"session-cookie",

			//	"/",
			//	"stat.vybory.gov.ru"));
			handler.CookieContainer = container;
			_httpClient = new HttpClient(handler);
			_ownsHttpClient = true;
		}

		public ApiClient(HttpClient httpClient,bool ownsHttpClient)
		{
			_httpClient = httpClient;
			_ownsHttpClient = ownsHttpClient;
		}

		~ApiClient()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_ownsHttpClient)
			{
				_httpClient?.Dispose();
			}
			
		}

		private Uri GetAbsoluteUrl(string path)
		{
			return new Uri(_baseUrl, path);
		}

		public void SetAuthorization(string auth)
		{
			_bearerToken = auth;
		}

		private async Task<HttpResponseMessage> GetResponse(HttpMethod httpMethod, string path,bool auth)
		{
			await Task.Delay(200);
			var uri = GetAbsoluteUrl(path);
			using var httpRequestMessage = new HttpRequestMessage(httpMethod, uri);
			if (auth)
			{
				httpRequestMessage.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _bearerToken);
			}
			return await _httpClient.SendAsync(httpRequestMessage);
		}

		public async Task<(string,string)> GetCertificate()
		{
			string subject = null;
			string issuer = null;
			using var handler = new HttpClientHandler();
			X509Certificate ret = null;
			handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, error) => {
				subject = cert.Subject;
				issuer = cert.Issuer;
				return true;
			};

			try
			{
				using (HttpClient client = new HttpClient(handler))
				{
					using (HttpResponseMessage response = await client.GetAsync("https://stat.vybory.gov.ru/"))
					{
						using (HttpContent content = response.Content)
						{
						}
					}
				}
			}
			catch (Exception)
			{

			}
			
			return (subject, issuer);
		}

		private async Task<T> PerformRequestAsync<T>(string path,bool auth=false)
		{
			using var httpResponseMessage = await GetResponse(HttpMethod.Get, path, auth);
#if DEBUG
			var json = await httpResponseMessage.Content.ReadAsStringAsync();
			T ret;
			try
			{
				ret = JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error {0}", e.Message);
				throw;
			}
			return ret;
#else
			using var responseStream=await httpResponseMessage.Content.ReadAsStreamAsync();
			return await JsonSerializer.DeserializeAsync<T>(responseStream, _jsonSerializerOptions);
#endif
		}

		public Task<BaseResponse<GetElectionsResponse>> GetElections(long regionCode)
		{
			return PerformRequestAsync<BaseResponse<GetElectionsResponse>>("api/elections?regionCode=" + regionCode);
		}

		public Task<dynamic> GetElectionStatistics(long regionCode,string electionId)
		{
			// нафиг не нужно для наших целей
			var path = $"api/statistics/election?electionId={electionId}&regionCode={regionCode}";
			return PerformRequestAsync<dynamic>(path);
		}

		public Task<BaseResponse<District[]>> GetDistricts(long regionCode, string electionId)
		{
			var path = $"api/elections/districts?regionCode={regionCode}&electionId={electionId}";
			return PerformRequestAsync<BaseResponse<District[]>>(path);
		}

		public Task<BaseResponse<DistrictVotingResponse>> GetDistrictsVotings(string districtId)
		{
			var path = $"api/statistics/voting?districtId=" + districtId;
			return PerformRequestAsync<BaseResponse<DistrictVotingResponse>>(path);
		}

		public async Task<BaseResponse<string[]>> GetContractTransactionsFiles(string contractId)
		{
			var newPath = $"api/files/" + contractId;
			//var path = $"api/transactions/filenames/" + contractId;
			return await PerformRequestAsync<BaseResponse<string[]>>(newPath, true);
		}

		public async Task<byte[]> DownloadTransactionFile(string contractId,string fileName)
		{
			await Task.Delay(200);
			var newPath = $"api/files/{fileName}/contract/{contractId}/download";
			//var path = $"download/{contractId}/{fileName}";
			using var response = await GetResponse(HttpMethod.Post, newPath, true);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsByteArrayAsync();
		}
	}
}
