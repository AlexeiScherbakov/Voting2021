using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using OxyPlot;
using OxyPlot.Wpf;

using Voting2021.BlockchainWatcher.ResearchDatabase;

using VotingFilesDownloader.Database;
using NHibernate;
using NHibernate.SqlCommand;
using System.Diagnostics;

namespace Analyzer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
		: Window
	{
		private ResearchDatabase _researchDatabase;

		private readonly BlockTimestampModelHelper _blockTimestampModelHelper = new();
		private readonly BlockTimestampModelHelper _blockTimestampModelHelper2 = new();
		private readonly TransactionsInBlockModelHelper _transactionsInBlockModelHelper = new();
		private readonly TransactionsInBlockPerTimeModelHelper _transactionsInBlockPerTimeModelHelper = new();
		public MainWindow()
		{
			InitializeComponent();

			this.Loaded += MainWindow_Loaded;

			blockTimestamp.Model = _blockTimestampModelHelper.PlotModel;
			blockTimestamp2.Model = _blockTimestampModelHelper2.PlotModel;
			transactionsInBlock.Model = _transactionsInBlockModelHelper.PlotModel;
			transactionsInBlockPerTime.Model = _transactionsInBlockPerTimeModelHelper.PlotModel;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private async void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog o = new OpenFileDialog();
			o.Filter = "*.db3|*.db3";
			if (o.ShowDialog(this) != true)
			{
				return;
			}

			System.Data.SQLite.SQLiteConnectionStringBuilder b = new();
			b.BinaryGUID = true;
			b.ForeignKeys = true;
			b.DataSource = o.FileName;
			var connectionString = b.ToString();
			_researchDatabase = ResearchDatabase.Sqlite(connectionString, false);
			busy.IsBusy = true;
			await Task.Factory.StartNew(LoadData, TaskCreationOptions.LongRunning);
			busy.IsBusy = false;
		}

		public void LoadData()
		{
			try
			{
				LoadDataUnsafe();
			}
			catch(Exception e)
			{

			}
		}

		private void LoadDataUnsafe()
		{
			using var session = _researchDatabase.SessionFactory
				.WithOptions()
				.OpenSession();

			var blockTimeData = session.Query<DbBlockchainBlock>()
				.Select(x => new { x.Timestamp, x.Shard, x.Height, TxCount = x.Transactions.Count() })
				.ToArray();

			var shardTimeDatas = blockTimeData.GroupBy(x => x.Shard)
				.Select(x => new
				{
					Shard = x.Key,
					TimeData = x.ToArray()
				});

			_blockTimestampModelHelper.PlotModel.Series.Clear();
			_blockTimestampModelHelper2.PlotModel.Series.Clear();
			_transactionsInBlockModelHelper.PlotModel.Series.Clear();
			Dispatcher.Invoke(() =>
			{
				blockTimeTabs.Items.Clear();
			});

			foreach (var shardTimeData in shardTimeDatas)
			{
				var shardName = "Shard #" + shardTimeData.Shard;
				var data = shardTimeData.TimeData.OrderBy(x => x.Height).Select(x => new BlockTimestampModelHelper.BlockTimestampPoint(x.Timestamp, x.Height));
				var dataWoGenesis = data.Where(x => x.BlockNumber > 1);

				_blockTimestampModelHelper.LoadData(shardName, data);
				_blockTimestampModelHelper2.LoadData(shardName, dataWoGenesis);

				BlockTimeModelHelper blockTimeModelHelper = new();
				Dispatcher.Invoke(() =>
				{
					TabItem tab = new TabItem();
					var plotView = new PlotView();
					tab.Header = shardName;
					tab.Content = plotView;
					plotView.Model = blockTimeModelHelper.PlotModel;
					blockTimeTabs.Items.Add(tab);
				});

				blockTimeModelHelper.LoadData(shardName, CreateBlockTimePoints(dataWoGenesis));
				blockTimeModelHelper.PlotModel.InvalidatePlot(true);

				var transactionCountData = shardTimeData.TimeData.OrderBy(x => x.Height).Select(x => new TransactionsInBlockModelHelper.BlockTransactionsPoint(x.TxCount, x.Height));
				_transactionsInBlockModelHelper.LoadData(shardName, transactionCountData);
				var transactionCountData2 = shardTimeData.TimeData.OrderBy(x => x.Height).Select(x => new TransactionsInBlockPerTimeModelHelper.BlockTransactionsPoint(x.TxCount, x.Timestamp));
				_transactionsInBlockPerTimeModelHelper.LoadData(shardName, transactionCountData2);
			}
			_blockTimestampModelHelper.PlotModel.InvalidatePlot(true);
			_blockTimestampModelHelper2.PlotModel.InvalidatePlot(true);
			_transactionsInBlockModelHelper.PlotModel.InvalidatePlot(true);
			_transactionsInBlockPerTimeModelHelper.PlotModel.InvalidatePlot(true);

			// Отдельно грузим типы транзакций (это долго из-за тормозов sqlite)
			var transactionByTypes = session.Query<DbBlockchainTransaction>()
				.GroupBy(x => new { x.OperationType, x.Block.Id })
				.Select(x => new
				{
					BlockId = x.Key.Id,
					Type = x.Key.OperationType,
					Count = x.Count()
				}).ToArray();

			var blocksData = session.Query<DbBlockchainBlock>()
				.FetchMany(x => x.Transactions)
				.Where(x => x.Transactions.Count > 0)
				.Select(x => new
				{
					x.Id,
					x.Shard,
					x.Height,
					x.Timestamp
				});

			var trTypesJoined = transactionByTypes.Join(blocksData, x => x.BlockId, x => x.Id, (x, y) =>
					new
					{
						y.Shard,
						y.Height,
						y.Timestamp,
						x.Type,
						x.Count
					}).ToArray();

			var perShardTrTypes = trTypesJoined.GroupBy(x => x.Shard)
				.Select(x => new
				{
					Shard = x.Key,
					Data = x.ToArray()
				});

			foreach(var item in perShardTrTypes)
			{
				var shardName = "Shard #" + item.Shard;

				TransactionsInBlockModelHelper perBlock = new();
				TransactionsInBlockPerTimeModelHelper perTime = new();
				Dispatcher.Invoke(() =>
				{
					{
						TabItem tab = new TabItem();
						var plotView = new PlotView();
						tab.Header = shardName;
						tab.Content = plotView;
						plotView.Model = perBlock.PlotModel;
						transactionInBlocksTabs.Items.Add(tab);
					}
					{
						TabItem tab = new TabItem();
						var plotView = new PlotView();
						tab.Header = shardName;
						tab.Content = plotView;
						plotView.Model = perTime.PlotModel;
						transactionInBlocksTimeTabs.Items.Add(tab);
					}
				});

				foreach(var byType in item.Data.GroupBy(x=>x.Type).Select(x=> new { x.Key, Data = x.ToArray() }))
				{
					perBlock.LoadData(byType.Key, byType.Data.Select(x => new TransactionsInBlockModelHelper.BlockTransactionsPoint(x.Count, x.Height)));
					perTime.LoadData(byType.Key, byType.Data.Select(x => new TransactionsInBlockPerTimeModelHelper.BlockTransactionsPoint(x.Count, x.Timestamp)));
				}

				perBlock.PlotModel.InvalidatePlot(true);
				perTime.PlotModel.InvalidatePlot(true);
			}
		}


		private IEnumerable<BlockTimeModelHelper.BlockTimePoint> CreateBlockTimePoints(IEnumerable<BlockTimestampModelHelper.BlockTimestampPoint> datawoGenesis)
		{
			BlockTimestampModelHelper.BlockTimestampPoint? last = null;
			foreach(var item in datawoGenesis)
			{
				if (last != null)
				{
					long number = last.Value.BlockNumber;
					var count = item.BlockNumber - number;
					var duration = item.DateTime - last.Value.DateTime;
					var blocktime = duration / count;
					yield return new BlockTimeModelHelper.BlockTimePoint(blocktime, number);
				}
				last = item;
			}
		}
	}
}
