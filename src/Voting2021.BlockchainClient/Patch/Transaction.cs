using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Voting2021.BlockchainClient.ObjectModel;

namespace WavesEnterprise
{
	partial class Transaction
	{

		public byte[] GetTransactionId()
		{
			return ((ITransactionWithId) transaction_).Id.ToByteArray();
		}

		public long GetTimestamp()
		{
			return ((ITransactionWithId) transaction_).Timestamp;
		}
	}
}
