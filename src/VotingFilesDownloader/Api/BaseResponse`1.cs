using System.Text.Json.Serialization;

namespace VotingFilesDownloader.Api
{
	public sealed class BaseResponse<T>
	{
		[JsonPropertyName("data")]
		public T Data { get; set; }

		[JsonPropertyName("error")]
		public ErrorInfo Error { get; set; }
	}


	public sealed class ErrorInfo
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("description")]
		public string Description { get; set; }
	}
}
