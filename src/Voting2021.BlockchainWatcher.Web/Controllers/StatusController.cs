using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

using Voting2021.BlockchainWatcher.Services;
using Voting2021.BlockchainWatcher.Web.Services;

namespace Voting2021.BlockchainWatcher.Web.Controllers
{
	[Route("api/v1/status")]
	public class StatusController
		: ControllerBase
	{
		private readonly StatusService _statusService;

		public StatusController(StatusService statusService)
		{
			_statusService = statusService;
		}


		[HttpGet]
		[Route("")]
		public BaseResponse<StatusResponse> GetStatus()
		{
			return new BaseResponse<StatusResponse>()
			{
				Data = new StatusResponse()
				{
					CurrentHeight = _statusService.GetCurrentHeight(),
					TransactionCount = _statusService.GetTotalTransactions()
				},
				Success = true
			};
		}
	}

	public sealed class BaseResponse<T>
	{
		[JsonPropertyName("data")]
		public T Data { get; set; }

		[JsonPropertyName("success")]
		public bool Success { get; set; }
	}

	public sealed class StatusResponse
	{
		[JsonPropertyName("currentHeight")]
		public long CurrentHeight { get; set; }

		[JsonPropertyName("transactionCount")]
		public long TransactionCount { get; set; }
	}
}
