using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Voting2021.BlockchainWatcher.Services;

namespace Voting2021.BlockchainWatcher.Web.Services
{
	public class StatusService
	{
		private readonly BlockchainWatcherHostedService _hostedService;
		private readonly IBlockchainEventProcessor _eventProcessor;

		public StatusService(BlockchainWatcherHostedService hostedService, IBlockchainEventProcessor eventProcessor)
		{
			_hostedService = hostedService;
			_eventProcessor = eventProcessor;
		}

		public long GetCurrentHeight()
		{
			return _hostedService.CurrentHeight;
		}

		public long GetTotalTransactions()
		{
			return _eventProcessor.GetLastProcessedBlockInfo().transactionCount;
		}
	}
}
