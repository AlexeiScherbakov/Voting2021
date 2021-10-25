
using System;
using System.Collections.Generic;

using OxyPlot;
using OxyPlot.Axes;

namespace Analyzer
{
	public sealed class BlockTimeModelHelper
		: BasePlotModelHelper
	{
		private LinearAxis _xAxis;
		private TimeSpanAxis _yAxis;

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
			_yAxis = new TimeSpanAxis()
			{
				Title = "Время вычисления блока"
			};
			_yAxis.AbsoluteMinimum = TimeSpanAxis.ToDouble(TimeSpan.Zero);
			_yAxis.Minimum = TimeSpanAxis.ToDouble(TimeSpan.Zero);
			return _yAxis;
		}

		public void LoadData(string name, IEnumerable<BlockTimePoint> points)
		{
			lock (PlotModel.SyncRoot)
			{
				OxyPlot.Series.LineSeries lineSeries = new()
				{
					Title = name,
				};
				foreach (var point in points)
				{
					var y = TimeSpanAxis.ToDouble(point.BlockCalculationTime);
					lineSeries.Points.Add(new DataPoint(point.BlockNumber, y));
				}
				PlotModel.Series.Add(lineSeries);
			}
		}


		public readonly struct BlockTimePoint
		{
			public readonly TimeSpan BlockCalculationTime;
			public readonly long BlockNumber;

			public BlockTimePoint(TimeSpan blockCalculationTime, long blockNumber)
			{
				BlockCalculationTime = blockCalculationTime;
				BlockNumber = blockNumber;
			}
		}
	}
}
