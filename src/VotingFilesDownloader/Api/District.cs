using System.Text.Json.Serialization;

namespace VotingFilesDownloader.Api
{

	public sealed class District
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("number")]
		public int Number { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }
	}
}
