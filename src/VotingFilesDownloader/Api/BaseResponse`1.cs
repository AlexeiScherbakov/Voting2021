using System.Text.Json.Serialization;

namespace VotingFilesDownloader.Api
{
	public sealed class BaseResponse<T>
	{
		[JsonPropertyName("data")]
		public T Data { get; set; }
	}
}
