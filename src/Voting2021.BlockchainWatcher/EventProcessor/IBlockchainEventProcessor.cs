namespace Voting2021.BlockchainWatcher.Services
{


	public interface IBlockchainEventProcessor
	{


		Database.StateStore StateStore { get; }

		/// <summary>
		/// Получает информацию о последнем обработанном блоке
		/// </summary>
		/// <returns></returns>
		(byte[] signature, long height,long transactionCount) GetLastProcessedBlockInfo();


		void ProcessBlockAppended(WavesEnterprise.BlockAppended blockAppended);

		void ProcessAppendedBlockHistory(WavesEnterprise.AppendedBlockHistory appendedBlockHistory);

		public void ProcessMicroBlockAppended(WavesEnterprise.MicroBlockAppended microBlockAppended);

		public void ProcessRollbackCompleted(WavesEnterprise.RollbackCompleted rollbackCompleted);
	}
}
