using CommandLine;

namespace VotingFilesDownloader
{
	public sealed class CommandLineOptions
	{
		[Option("token")]
		public string Token { get; set; }

		[Option("redownloadAll")]
		public bool RedownloadAll { get; set; }

		[Option("contractId")]
		public string ContractId { get; set; }
	}
}
