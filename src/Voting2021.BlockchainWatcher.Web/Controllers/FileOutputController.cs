using System.IO;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

using Voting2021.BlockchainWatcher.Services;
using Voting2021.BlockchainWatcher.Web.Services;

namespace Voting2021.BlockchainWatcher.Web.Controllers
{


	[Route("api/v1/fileOutput")]
	public class FileOutputController
		: ControllerBase
	{
		private readonly FileOutputService _fileOutputService;

		public FileOutputController(FileOutputService fileOutputService)
		{
			_fileOutputService = fileOutputService;
		}


		[HttpPost]
		[Route("start")]
		public BaseResponse<string> Start([FromBody] StartRequest request)
		{
			if (Directory.Exists(request.Path1) && Directory.Exists(request.Path2))
			{
				_fileOutputService.StartProcess(request.Path1, request.Path2);
				return new BaseResponse<string>()
				{
					Success = true
				};
			}
			else
			{
				return new BaseResponse<string>()
				{
					Success = false
				};
			}
		}

		[HttpPost]
		[Route("stop")]
		public BaseResponse<string> Stop([FromBody] StartRequest request)
		{
			_fileOutputService.Stop();
			return new BaseResponse<string>()
			{
				Success = true
			};
		}
	}


	public sealed class StartRequest
	{
		[JsonPropertyName("path1")]
		public string Path1 { get; set; }

		[JsonPropertyName("path2")]
		public string Path2 { get; set; }
	}
}
