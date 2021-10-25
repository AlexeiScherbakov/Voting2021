using System;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using WavesEnterprise;

namespace Voting2021.BlockchainClient
{
	public class EventStreamReader
		:IDisposable
	{
		private GrpcChannel _channel;
		private WavesEnterprise.BlockchainEventsService.BlockchainEventsServiceClient _blockchainEventsServiceClient;

		private AsyncServerStreamingCall<BlockchainEvent> _grcStream;

		private string _apiKey = "test";

		public EventStreamReader(string address, byte[] lastBlockSignature)
		{
			
			//var credentials = CallCredentials.FromInterceptor((context, metadata) =>
			//{
			//	metadata.Add("X-API-Key", _apiKey);
			//	return Task.CompletedTask;
			//});
			_channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions()
			{
				MaxReceiveMessageSize= null//1024 * 1024 * 64
				//Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
			});
			_blockchainEventsServiceClient = new(_channel);


			var subscribeRequest = new SubscribeOnRequest();

			var txTypeFilter = new TxTypeFilter();
			txTypeFilter.TxTypes.Add(105);
			var eventFilter = new EventsFilter()
			{
				TxTypeFilter = txTypeFilter
			};
			subscribeRequest.EventsFilters.Add(eventFilter);
			if (lastBlockSignature is null)
			{
				subscribeRequest.GenesisBlock = new GenesisBlock();
			}
			else
			{
				subscribeRequest.BlockSignature = new BlockSignature()
				{
					LastBlockSignature = Google.Protobuf.ByteString.CopyFrom(lastBlockSignature)
				};

// Для тестов
//#warning не забыть закомментировать
//				subscribeRequest.CurrentEvent = new CurrentEvent();
			}
			Metadata metadata = new Metadata();
			metadata.Add("X-API-Key", _apiKey);
			_grcStream = _blockchainEventsServiceClient.SubscribeOn(subscribeRequest, headers: metadata);
		}

		~EventStreamReader()
		{
			_grcStream?.Dispose();
			_channel?.Dispose();
		}

		public void Dispose()
		{
			_grcStream?.Dispose();
			_channel?.Dispose();
			GC.SuppressFinalize(this);
		}

		public AsyncServerStreamingCall<BlockchainEvent> Stream
		{
			get { return _grcStream; }
		}
	}
}
