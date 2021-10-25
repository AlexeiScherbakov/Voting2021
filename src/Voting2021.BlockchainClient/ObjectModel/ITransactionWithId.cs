using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voting2021.BlockchainClient.ObjectModel
{
	public interface ITransactionWithId
	{
		Google.Protobuf.ByteString Id { get; }
		long Timestamp { get; }
	}
}

namespace WavesEnterprise
{
	
	partial class GenesisTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class GenesisPermitTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class GenesisRegisterNodeTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class RegisterNodeTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class CreateAliasTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class IssueTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class ReissueTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class BurnTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class LeaseTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class LeaseCancelTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class SponsorFeeTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class SetAssetScriptTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class DataTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class TransferTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class MassTransferTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class PermitTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class CreatePolicyTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class UpdatePolicyTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class PolicyDataHashTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class CreateContractTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class CallContractTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class ExecutedContractTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class DisableContractTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class UpdateContractTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class SetScriptTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}

	partial class AtomicTransaction
		: Voting2021.BlockchainClient.ObjectModel.ITransactionWithId
	{

	}


}
