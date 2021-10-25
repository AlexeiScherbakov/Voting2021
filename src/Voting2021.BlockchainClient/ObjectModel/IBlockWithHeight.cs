using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voting2021.BlockchainClient.ObjectModel
{
	public interface IBlockWithHeight
	{
		long Height { get; }
		long Timestamp { get; }

		Google.Protobuf.ByteString BlockSignature { get; }
	}
}


namespace WavesEnterprise
{
	partial class BlockAppended
		: Voting2021.BlockchainClient.ObjectModel.IBlockWithHeight
	{

	}

	partial class AppendedBlockHistory
		: Voting2021.BlockchainClient.ObjectModel.IBlockWithHeight
	{

	}
}
