
using System;
using System.Collections.Generic;

using OxyPlot;
using OxyPlot.Axes;

namespace Analyzer
{

	public sealed class TransactionsInBlockPerTimeModelHelper
		: BasePlotModelHelper
	{
		private DateTimeAxis _xAxis;
		private LinearAxis _yAxis;

		protected override Axis CreateXAxis()
		{
			_xAxis = new DateTimeAxis()
			{
				Title = "Время"
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
					Title = name
				};
				foreach (var point in points)
				{
					lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(point.Time), point.Transactions));
				}
				PlotModel.Series.Add(lineSeries);
			}
		}


		public readonly struct BlockTransactionsPoint
		{
			public readonly int Transactions;
			public readonly DateTime Time;

			public BlockTransactionsPoint(int transactions, DateTime time)
			{
				Transactions = transactions;
				Time = time;
			}
		}
	}
}
