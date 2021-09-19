using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VotingFilesDownloader.Api
{

	public sealed class Election
	{
		[JsonPropertyName("electionId")]
		public string ElectionId { get; set; }

		[JsonPropertyName("electionName")]
		public string ElectionName { get; set; }

		[JsonPropertyName("sortLevel")]
		public int SortLevel { get; set; }
	}
}
