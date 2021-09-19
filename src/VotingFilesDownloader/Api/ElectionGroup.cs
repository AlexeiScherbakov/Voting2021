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

	public sealed class ElectionGroup
	{
		[JsonPropertyName("level")]
		public string Level { get; set; }

		[JsonPropertyName("elections")]
		public Election[] Elections { get; set; }
	}
}
