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



	public sealed class GetElectionsResponse
	{
		[JsonPropertyName("regionCode")]
		public int RegionCode { get; set; }

		[JsonPropertyName("regionName")]
		public string RegionName { get; set; }

		[JsonPropertyName("elections")]
		public ElectionGroup[] Elections { get; set; }
	}
}
