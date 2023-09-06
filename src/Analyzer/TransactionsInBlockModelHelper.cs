
using System;
using System.Collections.Generic;

using OxyPlot;
using OxyPlot.Axes;

namespace Analyzer
{
	public sealed class TransactionsInBlockModelHelper
		: BasePlotModelHelper
	{
		private LinearAxis _xAxis;
		private LinearAxis _yAxis;

		protected override Axis CreateXAxis()
		{
			_xAxis = new LinearAxis()
			{
				Title = "Номер блока"
			};
			return _xAxis;
		}

		protected override Axis CreateYAxis()
		{
			_yAxis = new LinearAxis()
			{
				Title = "Количество транзакций"
			};
			_yAxis.AbsoluteMinimum = 0;
			_yAxis.Minimum = 0;
			return _yAxis;
		}

		public void LoadData(string name, IEnumerable<BlockTransactionsPoint> points)
		{
			lock (PlotModel.SyncRoot)
			{
				OxyPlot.Series.LineSeries lineSeries = new()
				{
					Title = name,
					CanTrackerInterpolatePoints = false
				};
				foreach (var point in points)
				{
					lineSeries.Points.Add(new DataPoint(point.BlockNumber, point.Transactions));
				}
				PlotModel.Series.Add(lineSeries);
			}
		}


		public readonly struct BlockTransactionsPoint
		{
			public readonly int Transactions;
			public readonly long BlockNumber;

			public BlockTransactionsPoint(int transactions, long blockNumber)
			{
				Transactions = transactions;
				BlockNumber = blockNumber;
			}
		}
	}
}
