using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Voting2021.BlockchainWatcher.Web.Services;

namespace Voting2021.BlockchainWatcher.Web.Controllers
{
	[Route("api/copyTransactions")]
	[ApiController]
	public class CopyTransactionController
		: ControllerBase
	{
		private TransactionsCopyService _transactionsCopyService;

		public CopyTransactionController(TransactionsCopyService transactionCopyService)
		{
			_transactionsCopyService = transactionCopyService;
		}

		[HttpPost]
		[Route("")]
		public BaseResponse<CopyTransactionResponse> CopyTransaction([FromBody] CopyTransactionRequest request)
		{
			var ret = _transactionsCopyService.CopyFileOperation(request.FileName, request.StartBlock, request.EndBlock);
			return new BaseResponse<CopyTransactionResponse>()
			{
				Data = new CopyTransactionResponse()
				{
					AlreadyStarted = ret.Item1,
					ProgressCurrent = ret.Item2,
					ProgressTotal = ret.Item3
				},
				Success = true
			};
		}
	}

	public sealed class CopyTransactionRequest
	{
		[JsonPropertyName("fileName")]
		public string FileName { get; set; }

		[JsonPropertyName("startBlock")]
		public long StartBlock { get; set; }

		[JsonPropertyName("endBlock")]
		public long EndBlock { get; set; }
	}


	public sealed class CopyTransactionResponse
	{
		[JsonPropertyName("alreadyStarted")]
		public bool AlreadyStarted { get; set; }

		[JsonPropertyName("progressCurrent")]
		public long ProgressCurrent { get; set; }

		[JsonPropertyName("progressTotal")]
		public long ProgressTotal { get; set; }
	}
}
