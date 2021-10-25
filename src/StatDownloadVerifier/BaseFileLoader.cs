using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Voting2021.FilesUtils;

namespace StatDownloadVerifier
{
	public abstract class BaseFileLoader
	{
		public abstract void AddFile(string fileName);

		public abstract Task WaitUntilCompletedAsync();
	}

	public abstract class BaseFileLoader<T>
		:BaseFileLoader
	{
		private readonly BufferBlock<string> _fileNameQueueBlock;
		private readonly TransformBlock<string, (string, byte[])> _fileLoadBlock;
		private readonly TransformBlock<(string, byte[]), (string, T)> _fileTransformBlock;
		private readonly BatchBlock<(string, T)> _batchBlock;
		private readonly ActionBlock<(string, T)[]> _finalAggregateBlock;

		public BaseFileLoader()
		{
			_fileNameQueueBlock = new BufferBlock<string>(new DataflowBlockOptions()
			{
				EnsureOrdered = true
			});
			_fileLoadBlock = new TransformBlock<string, (string, byte[])>(LoadFileBlock, new ExecutionDataflowBlockOptions()
			{
				MaxDegreeOfParallelism = 1,
				EnsureOrdered = true,
				BoundedCapacity = 5000
			});
			_fileTransformBlock = new TransformBlock<(string, byte[]), (string, T)>(ProcessFileBlockFunc, new ExecutionDataflowBlockOptions()
			{
				MaxDegreeOfParallelism = 8,
				EnsureOrdered = true,
				BoundedCapacity = 5000
			});
			_batchBlock = new BatchBlock<(string, T)>(1000, new GroupingDataflowBlockOptions()
			{
				BoundedCapacity = 1000
			});
			_finalAggregateBlock = new ActionBlock<(string, T)[]>(AccumulateBlockFunc, new ExecutionDataflowBlockOptions()
			{
				BoundedCapacity = 1,
				MaxDegreeOfParallelism = 1,
				EnsureOrdered = false
			});

			_fileNameQueueBlock.LinkTo(_fileLoadBlock);
			_fileLoadBlock.LinkTo(_fileTransformBlock);
			_fileTransformBlock.LinkTo(_batchBlock);
			_batchBlock.LinkTo(_finalAggregateBlock);
		}

		public override sealed void AddFile(string fileName)
		{
			_fileNameQueueBlock.Post(fileName);
		}

		public override sealed async Task WaitUntilCompletedAsync()
		{
			await WaitOnBlock(_fileNameQueueBlock);
			await WaitOnBlock(_fileLoadBlock);
			await WaitOnBlock(_fileTransformBlock);
			_batchBlock.TriggerBatch();
			await WaitOnBlock(_batchBlock);
			await WaitOnBlock(_finalAggregateBlock);
		}

		private async Task WaitOnBlock<TBlock>(TBlock block)
			where TBlock: IDataflowBlock
		{
			block.Complete();
			await block.Completion;
		}

		private (string, byte[]) LoadFileBlock(string fileName)
		{
			OnFileLoading(fileName);
			using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.SequentialScan);
			var len = fileStream.Length;
			byte[] buffer = new byte[len];
			Memory<byte> memBuffer = buffer;
			while (memBuffer.Length > 0)
			{
				int count = fileStream.Read(memBuffer.Span);
				memBuffer = memBuffer.Slice(count);
			}
			return (fileName, buffer);
		}

		protected virtual void OnFileLoading(string fileName)
		{

		}


		private (string, T) ProcessFileBlockFunc((string, byte[]) fileData)
		{
			return ProcessFileBlock(fileData);
		}

		protected abstract (string, T) ProcessFileBlock((string, byte[]) fileData);

		private void AccumulateBlockFunc((string, T)[] processedFileData)
		{
			AccumulateBlock(processedFileData);
		}

		protected abstract void AccumulateBlock((string, T)[] processedFileData);
	}
}
