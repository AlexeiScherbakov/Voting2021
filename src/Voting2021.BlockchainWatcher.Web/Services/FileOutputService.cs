using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Voting2021.BlockchainWatcher.Services;
using Voting2021.BlockchainWatcher.Settings;

namespace Voting2021.BlockchainWatcher.Web.Services
{
	public class FileOutputService
	{
		private ILogger<FileOutputService> _logger;


		private Task _task;
		private CancellationTokenSource _cancellationTokeSource = new CancellationTokenSource();

		private string _appId;

		private TransactionsCopyService _transactionCopyService;

		private IDataSigningService _dataSigningService;

		private IBlockchainEventProcessor _blockchainEventProcessor;

		private string _path1;
		private string _path2;


		private volatile int _isStarted = 0;

		public FileOutputService(
			ILogger<FileOutputService> logger,
			TransactionsCopyService transactionsCopyService,
			IOptions<AppIdSettings> appIdOptions,
			IDataSigningService dataSigningService,
			IBlockchainEventProcessor blockchainEventProcessor)
		{
			_logger = logger;
			_appId = appIdOptions.Value.AppId;
			_transactionCopyService = transactionsCopyService;
			_dataSigningService = dataSigningService;
			_blockchainEventProcessor = blockchainEventProcessor;
		}


		public void StartProcess(string path1,string path2)
		{
			if (_isStarted!=0)
			{
				return;
			}
			_isStarted = 1;
			_path1 = path1;
			_path2 = path2;
			_cancellationTokeSource = new CancellationTokenSource();
			_task = Task.Factory.StartNew(async () =>
			{
				await FileCopyProcessTask();
				_isStarted = 0;
			});
		}


		public void Stop()
		{
			_cancellationTokeSource.Cancel();
		}

		public async Task FileCopyProcessTask()
		{
			DateTime? _lastFileCopy = null;
			long lastBlock = 0;
			while (!_cancellationTokeSource.IsCancellationRequested)
			{
				bool needToCopy = false;
				if (_lastFileCopy is null)
				{
					needToCopy = true;
				}
				else
				{
					var delta = DateTime.Now - _lastFileCopy.Value;
					if (delta > TimeSpan.FromMinutes(5))
					{
						needToCopy = true;
					}
				}

				if (needToCopy)
				{
					_logger.LogInformation("Coping transactions to files");
					var info = _blockchainEventProcessor.GetLastProcessedBlockInfo();

					var fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")+"-" + lastBlock + "-" + info.height;

					var folder1 = Path.Combine(_path1, _appId);
					var folder2 = Path.Combine(_path2, _appId);

					var file1 = Path.Combine(_path1, _appId, fileName);
					var file2= Path.Combine(_path2, _appId, fileName);

					try
					{
						if (!Directory.Exists(folder1))
						{
							Directory.CreateDirectory(folder1);
						}
						await _transactionCopyService.CopyToAsync(file1, lastBlock, info.height);
						_dataSigningService.SignFile(file1);
					}
					catch(Exception e)
					{
						_logger.LogError(e,"Error in copy operation");
					}

					try
					{
						if (!Directory.Exists(folder2))
						{
							Directory.CreateDirectory(folder2);
						}
						await _transactionCopyService.CopyToAsync(file2, lastBlock, info.height);
						_dataSigningService.SignFile(file2);
					}
					catch (Exception e)
					{
						_logger.LogError(e, "Error in copy operation");
					}
					_lastFileCopy = DateTime.Now;
					lastBlock = info.height;
				}

				try
				{
					await Task.Delay(60000);
				}
				catch(Exception e)
				{
					_logger.LogInformation("File Copy process exited");
				}
			}
		}
	}
}
