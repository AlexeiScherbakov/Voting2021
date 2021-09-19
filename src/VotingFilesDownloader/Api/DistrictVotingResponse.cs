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

	public sealed class DistrictVotingResponse
	{

		[JsonPropertyName("votings")]
		public DistrictVoting[] Votings { get; set; }


		public sealed class DistrictVoting
		{
			[JsonPropertyName("type")]
			public string Type { get; set; }

			[JsonPropertyName("counters")]
			public DistrictVotingCounters Counters { get; set; }
		}

		public sealed class DistrictVotingCounters
		{
			[JsonPropertyName("contractId")]
			public string ContractId { get; set; }

			[JsonPropertyName("name")]
			public string Name { get; set; }
		}
	}
}
