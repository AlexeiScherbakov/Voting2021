using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

using Voting2021.BlockchainWatcher.Services;
using Voting2021.BlockchainWatcher.Web.Services;

namespace Voting2021.BlockchainWatcher.Web.Controllers
{

	[Route("api/v1/sign")]
	public class SignController
		: ControllerBase
	{
		private readonly IDataSigningService _dataSigningService;

		public SignController(IDataSigningService dataSigningService)
		{
			_dataSigningService = dataSigningService;
		}


		[HttpPost]
		[Route("")]
		public BaseResponse<StatusResponse> GetStatus([FromBody] SignRequest request)
		{
			_dataSigningService.SignFile(request.FileName);
			return new BaseResponse<StatusResponse>()
			{
				Success = true
			};
		}
	}

	public sealed class SignRequest
	{
		[JsonPropertyName("fileName")]
		public string FileName { get; set; }
	}
}
